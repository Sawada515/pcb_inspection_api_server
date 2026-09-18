/**
 *  @file Services/ExternalProcessGateway.cs
 *  @brief ExternalProcessGateway class for managing external processes
 */

using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;

using api_server.Models;
using api_server.Models.Enums;
using api_server.Services;

using api_server.Models.DatabaseType;
using api_server.Models.DatabaseType.TableRecord;

namespace api_server.Services;

public class ExternalProcessGateway
{
    readonly string _baseUrl;
    readonly string _realm;
    readonly string _clientId;
    readonly string _clientSecret;

    private readonly SyslogClient _syslogClient;
    private readonly UnixDomainSocketClient _unixDomainSocketClient;

    /**
     * @brief リクエストIDのカウンタ
     * @note 使用したらインクリメント 
     */
    private int _requestId = 0;

    public ExternalProcessGateway(IOptions<KeycloakOptions> options, SyslogClient syslogClient, UnixDomainSocketClient unixDomainSocketClient)
    {
        var keycloakOptions = options.Value;

        _baseUrl = keycloakOptions.BaseUrl;
        _realm = keycloakOptions.Realm;
        _clientId = keycloakOptions.ClientId;
        _clientSecret = keycloakOptions.ClientSecret;

        _syslogClient = syslogClient;
        _unixDomainSocketClient = unixDomainSocketClient;
    }

    public async Task<ushort> GetSlotStatus()
    {
        ushort slotStatus = 0x0000;

        const int SlotRowMax = 3;
        const int SlotColMax = 3;

        await _unixDomainSocketClient.ConnectAsync();

        // slotの個数分問い合わせ
        for (int i = 0; i < SlotColMax; ++i)
        {
            for (int j = 0; j < SlotRowMax; ++j)    
            {
                var slotStatusReadBodyData = new StoreData (
                    //Col: i,
                    //Row: j
                    StoreId: (i * SlotRowMax) + j
                );

                var readSlotStatusRequest = new DatabaseRequest<StoreData> (
                    requestId: _requestId++,
                    queryType: QueryTypeRecord.Read,
                    resource: ResourceRecord.StoreService,
                    data: slotStatusReadBodyData
                );

                byte[] requestJson = JsonSerializer.SerializeToUtf8Bytes(readSlotStatusRequest);

                var response = await AccessDaoSystemAsync<StoreData>(requestJson);
                if (response == null)
                {
                    await _syslogClient.SyslogAsync(
                        LogLevel.Error,
                        "Failed to get slot status"
                    );

                    continue;
                }
                if (response.IsSuccess)
                {
                    var slotData = response.Payload;
                    if (slotData != null && slotData.StoreStatus.HasValue)
                    {
                        int bitPosition = (i * SlotRowMax) + j;

                        if (slotData.StoreStatus.Value != StoreStatusRecord.Empty)
                        {
                            slotStatus |= (ushort)(1 << bitPosition);
                        }
                    }
                }

            }
        }

        return slotStatus;
    }

    public async Task<string> GetSystemStatus()
    {
        // 実際の実装では、死活監視システムにHTTPで問い合わせに行く

        SystemStatus status = SystemStatus.normal;
        var system_status = new
        {
            CollaborationRobotsStatus = status.ToString(),
            CollaborativeRobots_PLC = status.ToString(),
            PLC_Jetson = status.ToString(),
            Jetson_Rpi = status.ToString()
        };

        return System.Text.Json.JsonSerializer.Serialize(system_status);
    }

    public async Task<string> GetApplicationLog(int lines)
    {
        string ServiceName = _syslogClient.ServiceName;
        //string ServiceName = "notediscovery.service";

        var startInfo = new ProcessStartInfo
        {
            FileName = "journalctl",
            Arguments = $"-eu {ServiceName} -n {lines} --no-pager",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true       
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to start journalctl process"
            );

            return string.Empty;
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    public async Task<List<string>> GetSlotSubscribeInfo()
    {
        // DBに問い合わせ

        return new List<string>();
    }

    public async Task<string?> CreateUser(CreateUserData request_data)
    {
        var httpClient = new HttpClient();
        string uuid = string.Empty;

        // KeycloakのAPIを使用してユーザーを作成する
        var keycloakAccess = new KeycloakAccess(
            httpClient: httpClient,
            baseUrl: _baseUrl,
            realm: _realm,
            clientId: _clientId,
            clientSecret: _clientSecret
        );

        try {
                await _syslogClient.SyslogAsync(
                    LogLevel.Informational,
                    "request create new User "
                );

                uuid = await keycloakAccess.CreateUserAsync(
                    request_data.Username,
                    request_data.Password
                );
        }
        catch (InvalidOperationException ex)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                ex.Message
            );

            return null;
        }

        return uuid;
    }

    public async Task<bool> DeleteUser(DeleteUserData username)
    {
        var httpClient = new HttpClient();

        // KeycloakのAPIを使用してユーザーを削除する
        var keycloakAccess = new KeycloakAccess(
            httpClient: httpClient,
            baseUrl: _baseUrl,
            realm: _realm,
            clientId: _clientId,
            clientSecret: _clientSecret
        );

        try {
                await _syslogClient.SyslogAsync(
                    LogLevel.Informational,
                    $"Deleting user with username: {username}"
                );

                string? uuid = await keycloakAccess.GetUUIDFromUsernameAsync(username.username);
                if (string.IsNullOrEmpty(uuid))
                {
                    await _syslogClient.SyslogAsync(
                        LogLevel.Error,
                        $"User not found: {username}"
                    );

                    return false;
                }

                await keycloakAccess.DeleteUserAsync(uuid);
        }
        catch (InvalidOperationException ex)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                ex.Message
            );

            return false;
        }

        return true;
    }

    private async Task<DatabaseResponse<TResponse>?> AccessDaoSystemAsync<TResponse>(byte[] requestJson)
    {
        await _unixDomainSocketClient.ConnectAsync();

        await _unixDomainSocketClient.SendFrameAsync(requestJson);

        byte[]? responseBytes = await _unixDomainSocketClient.RecvFrameAsync();
        if (responseBytes == null)
        {
            return null;
        }

        await _syslogClient.SyslogAsync(
            LogLevel.Informational,
            $"response: {System.Text.Encoding.UTF8.GetString(responseBytes)}"
        );

        return JsonSerializer.Deserialize<DatabaseResponse<TResponse>>(responseBytes);
    }
}
