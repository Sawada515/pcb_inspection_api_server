/**
 * @file ExternalProcessGateway.cs
 * @brief 外部プロセスとの通信を行うゲートウェイクラス
 * @note Unixドメインソケットを使用して外部プロセスと通信する
 * @author Sawada
 */

using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;

using ApiServer.Models.Enums;

using ApiServer.Models;
using ApiServer.Models.DatabaseType;
using ApiServer.Models.DatabaseType.TableRecord;

namespace ApiServer.Services;

public class ExternalProcessGateway
{
    private readonly string _baseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private readonly SyslogClient _syslogClient;
    private readonly UnixDomainSocketClient _databaseSocket;

    /**
     * @brief リクエストIDのカウンタ
     * @note 使用したらインクリメント 
     * @warn 言語仕様として0で初期化済み
     */
    private int _requestId;

    public ExternalProcessGateway(IOptions<KeycloakOptions> options, SyslogClient syslogClient, UnixDomainSocketClient unixDomainSocketClient)
    {
        KeycloakOptions keycloakOptions = options.Value;

        _baseUrl = keycloakOptions.BaseUrl;
        _realm = keycloakOptions.Realm;
        _clientId = keycloakOptions.ClientId;
        _clientSecret = keycloakOptions.ClientSecret;

        Console.WriteLine($"client_secret: {_clientSecret}");
        Console.Out.Flush();

        _syslogClient = syslogClient;
        _databaseSocket = unixDomainSocketClient;
    }

    public async Task<SlotStatusResponse> GetSlotStatus()
    {
        const int SlotRowMax = 3;
        const int SlotColMax = 3;
        const int SlotMaxCount = SlotRowMax * SlotColMax;

        SlotStatusResponse slotStatus;
        IndividualSlotStatus[] individualSlotStatus = new IndividualSlotStatus[SlotMaxCount];

        await _databaseSocket.ConnectAsync();

        // slotの個数分問い合わせ
        for (int i = 0; i < SlotColMax; ++i)
        {
            for (int j = 0; j < SlotRowMax; ++j)
            {
                var slotStatusReadBodyData = new StoreData(
                    //Col: i,
                    //Row: j
                    StoreId: (i * SlotRowMax) + j
                );

                var readSlotStatusRequest = new DatabaseRequest<StoreData>(
                    requestId: _requestId++,
                    queryType: QueryTypeRecord.Read,
                    resource: ResourceRecord.StoreService,
                    data: slotStatusReadBodyData
                );

                byte[] requestJson = JsonSerializer.SerializeToUtf8Bytes(readSlotStatusRequest);

                var response = await AccessDaoSystemAsync<List<StoreData>>(requestJson);
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
                    if (response.Payload == null)
                    {
                        await _syslogClient.SyslogAsync(
                            LogLevel.Error,
                            "Response payload is null"
                        );

                        continue;
                    }
                    var slotData = response.Payload.FirstOrDefault();
                    if (slotData == null)
                    {
                        await _syslogClient.SyslogAsync(
                            LogLevel.Error,
                            "Slot data is null"
                        );

                        continue;
                    }

                    bool isStored = false;
                    string? userName;

                    if (slotData.StoreStatus.HasValue)
                    {
                        if (slotData.StoreStatus.Value != StoreStatusRecord.Empty)
                        {
                            isStored = true;
                        }
                    }

                    if (slotData.UserId == null)
                    {
                        userName = string.Empty;
                    }
                    else
                    {
                        userName = slotData.UserId;
                    }

                    individualSlotStatus[(i * SlotRowMax) + j] = new IndividualSlotStatus(userName, isStored);
                }
            }
        }

        slotStatus = new SlotStatusResponse(
            individualSlotStatus[0],
            individualSlotStatus[1],
            individualSlotStatus[2],
            individualSlotStatus[3],
            individualSlotStatus[4],
            individualSlotStatus[5],
            individualSlotStatus[6],
            individualSlotStatus[7],
            individualSlotStatus[8]
        );

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
        ///note journalctlをやめてrsyslogを使用するかも

        //string ServiceName = _syslogClient.ServiceName;
        string ServiceName = "notediscovery.service";

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

    public async Task<bool> RequestInspection(InspectionRequest request)
    {
        await _syslogClient.SyslogAsync(
            LogLevel.Informational,
            "Requesting inspection"
        );

        var status = request.ToStatus();

        var requestInspectionBodyData = new InspectionRequestData(
            RequestStatus: status
        );

        var requestInspectionRequest = new DatabaseRequest<InspectionRequestData>(
            requestId: _requestId++,
            queryType: QueryTypeRecord.Create,
            resource: ResourceRecord.InspectionRequestService,
            data: requestInspectionBodyData
        );

        var response = await CreateJsonAndAccessDaoSystemAsync<InspectionRequestData>(requestInspectionRequest);
        if (response == null)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to request inspection"
            );

            return false;
        }

        if (response.IsSuccess)
        {
            return true;
        }
        else
        {
            if (response.Error == null)
            {
                await _syslogClient.SyslogAsync(
                    LogLevel.Error,
                    "Failed to request inspection: Unknown error"
                );

                return false;
            }

            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                $"Failed to request inspection: {response.Error.Message}"
            );

            return false;
        }
    }

    // ユーザを実際に管理するのはKeycloak
    public async Task<string?> CreateUser(CreateUserRequest request)
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
                request.Username,
                request.Password
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

    // DBにユーザ情報を登録する
    public async Task<bool> UploadUserData(CreateUserRequest request, string uuid)
    {
        UserData userData = new UserData(
            UserId: request.Username,
            Role: UserRoleRecord.User,
            UUID: uuid,
            UserStatus: UserStatusRecord.Active
        );

        StoreData storeData = new StoreData(
            Col: request.Col,
            Row: request.Row,
            UserId: request.Username
        );

        var userDataCreateRequest = new DatabaseRequest<UserData>(
            requestId: _requestId++,
            queryType: QueryTypeRecord.Create,
            resource: ResourceRecord.UserService,
            data: userData
        );

        var responseUser = await CreateJsonAndAccessDaoSystemAsync<UserData>(userDataCreateRequest);
        if (responseUser == null || !responseUser.IsSuccess)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to upload user data"
            );

            return false;
        }

        var storeDataUpdateRequest = new DatabaseRequest<StoreData>(
            requestId: _requestId++,
            queryType: QueryTypeRecord.Update,
            resource: ResourceRecord.StoreService,
            data: storeData
        );

        var responseStore = await CreateJsonAndAccessDaoSystemAsync<StoreData>(storeDataUpdateRequest);
        if (responseStore == null || !responseStore.IsSuccess)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to upload store data"
            );
            return false;
        }

        return true;
    }

    // ここで削除するのはKeycloak上のユーザ
    public async Task<bool> DeleteUser(DeleteUserRequest request)
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
                $"Deleting user with username: {request}"
            );

            string? uuid = await keycloakAccess.GetUUIDFromUsernameAsync(request.Username);
            if (string.IsNullOrEmpty(uuid))
            {
                await _syslogClient.SyslogAsync(
                    LogLevel.Error,
                    $"User not found: {request}"
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

    public async Task<bool> DeleteUserData(DeleteUserRequest request)
    {
        UserData userData = new UserData(
            UserId: request.Username
        );

        var userDeleteRequest = new DatabaseRequest<UserData>(
            requestId: _requestId++,
            queryType: QueryTypeRecord.Delete,
            resource: ResourceRecord.UserService,
            data: userData
        );

        var responseUser = await CreateJsonAndAccessDaoSystemAsync<UserData>(userDeleteRequest);
        if (responseUser == null || !responseUser.IsSuccess)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to delete user data"
            );
            return false;
        }

        return true;
    }

    public async Task<UserListResponse?> GetUserList()
    {
        var dummyData = new List<UserResponse>();

        var userListRequest = new DatabaseRequest<List<UserResponse>>(
            requestId: _requestId++,
            queryType: QueryTypeRecord.UserList,
            resource: ResourceRecord.ComplexService,
            data: dummyData
        );

        var response = await CreateJsonAndAccessDaoSystemAsync<List<UserResponse>>(userListRequest);
        if (response == null || !response.IsSuccess || response.Payload == null)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to get user list"
            );

            return null;
        }

        return new UserListResponse(response.Payload);
    }

    private async Task<DatabaseResponse<TResponse>> CreateJsonAndAccessDaoSystemAsync<TResponse>(DatabaseRequest<TResponse> request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        byte[] requestJson = JsonSerializer.SerializeToUtf8Bytes(request);

        return await AccessDaoSystemAsync<TResponse>(requestJson);
    }

    private async Task<DatabaseResponse<TResponse>?> AccessDaoSystemAsync<TResponse>(byte[] requestJson)
    {
        await _databaseSocket.ConnectAsync();

        await _databaseSocket.SendFrameAsync(requestJson);

        byte[]? responseBytes;
        int itemCount;
        (responseBytes, itemCount) = await _databaseSocket.RecvFrameAsync();
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
