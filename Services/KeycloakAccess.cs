/**
 * @file    KeycloakAccess.cs
 * @brief   KeycloakAccess class for managing access tokens from Keycloak
 * @author  sawada 
 * @date    2026-09-02
 */

/**
 *  @note required install "dotnet add package Microsoft.Extensions.Http" for IHttpClientFactory
 */

using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace ApiServer.Services;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class KeycloakAccess
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private string? _cachedAccessToken = null;
    private DateTimeOffset _tokenExpiresAt;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public string BaseUrl => _baseUrl;
    public string Realm => _realm;
    public string ClientId => _clientId;

    /**
     * @breif コンストラクタ
     * @param[in] httpClient
     * @param[in] baseUrl
     * @param[in] realm
     * @param[in] clientId
     * @param[in] clientSecret 
     */
    public KeycloakAccess(HttpClient? httpClient, string baseUrl, string realm, string clientId, string clientSecret)
    {
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/');
        _realm = realm;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /**
     * @breif APIを使用するのに必要なアクセストークンを取得する
     */
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
        {
            return _cachedAccessToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            {
                return _cachedAccessToken;
            }

            var tokenEndpoint = $"{_baseUrl}/realms/{_realm}/protocol/openid-connect/token";

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(parameters)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                
                throw new InvalidOperationException($"Keycloak Token Error ({response.StatusCode}): {errorBody}");
            }
            response.EnsureSuccessStatusCode();

            var tokenResult = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
                                ?? throw new InvalidOperationException("Failed to retrieve access token.");

            _cachedAccessToken = tokenResult.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn);

            return _cachedAccessToken;
        }
        finally
        {
            _lock.Release();
        }
   }

    /**
     * @breif Keycloakにユーザーを作成する
     * @param[in] userName 作成するユーザー名
     * @param[in] password 作成するユーザーのパスワード
     * @return 作成されたユーザーのID
     */
    public async Task<string> CreateUserAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var usersEndpoint = $"{_baseUrl}/admin/realms/{_realm}/users";

        var payload = new
        {
            username = userName,
            enabled = true,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = password,
                    temporary = false
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, usersEndpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = JsonContent.Create(payload)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException($"User '{userName}' already exists.");
        }

        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(location))
        {
            throw new InvalidOperationException("Failed to retrieve the location of the created user.");
        }
        
        return location.TrimEnd('/').Split('/').Last();
    }

    /**
     * @breif Keycloakにユーザーを削除する
     * @param[in] uuid 削除するユーザーのID
     */
    public async Task DeleteUserAsync(string uuid, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var endpoint = $"{_baseUrl}/admin/realms/{_realm}/users/{uuid}";
    
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };
    
        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"can't delete user (ID: {uuid}) because it does not exist.");
        }
    
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetUUIDFromUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var token = GetAccessTokenAsync(cancellationToken);
        var endpoint = $"{_baseUrl}/admin/realms/{_realm}/users?username={Uri.EscapeDataString(username)}&exact=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token.Result) }
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<TokenResponse>>(cancellationToken: cancellationToken);
        if (users == null || users.Count == 0)
        {
            throw new InvalidOperationException($"User '{username}' not found.");
        }

        return users?.FirstOrDefault()?.Id;
    }
}
