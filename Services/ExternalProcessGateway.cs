/**
 *  @file Services/ExternalProcessGateway.cs
 *  @brief ExternalProcessGateway class for managing external processes
 */

using System.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;

using api_server.Models.Enums;

using api_server.Models;
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

    public async Task<SlotStatusData> GetSlotStatus()
    {
        const int SlotRowMax = 3;
        const int SlotColMax = 3;
        const int SlotMaxCount = SlotRowMax * SlotColMax;

        SlotStatusData slotStatus;
        IndividualSlotStatusData[] individualSlotStatus = new IndividualSlotStatusData[SlotMaxCount];

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
                        // require write log 
                    
                        continue;
                    }

                    bool isStored = false;
                    string? userName = string.Empty;

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

                    individualSlotStatus[(i * SlotRowMax) + j] = new IndividualSlotStatusData(userName, isStored);
                }
            }
        }

        slotStatus = new SlotStatusData(
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
        ///note journalctlをやめてrsyslogを使用する

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

        var requestInspectionBodyData = new InspectionRequestData (
            RequestStatus: status
        );

        var requestInspectionRequest = new DatabaseRequest<InspectionRequestData> (
            requestId: _requestId++,
            queryType: QueryTypeRecord.Create,
            resource: ResourceRecord.InspectionRequestService,
            data: requestInspectionBodyData
        );

        byte[] requestJson = JsonSerializer.SerializeToUtf8Bytes(requestInspectionRequest);

        var response = await AccessDaoSystemAsync<InspectionRequestData>(requestJson);
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

/*
    public async Task<bool> UploadUserData(string username, string uuid, int col, int row)
    {
        UserData userData = new UserData (
            UserId: username,
            Role: UserRoleRecord.User,
            UUID: uuid,
            UserStatus: UserStatusRecord.Active
        );

        StoreData storeData = new StoreData (
            Col: col,
            Row: row,
            UserId: username
        );

        // 途中
    }
    */

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

    public async Task<List<UserListData>> GetUserList()
    {
        var userListRequestBody = new UserData (
            UserStatus: UserStatusRecord.Active
        );

        var readSlotStatusRequest = new DatabaseRequest<UserData> (
            requestId: _requestId++,
            queryType: QueryTypeRecord.Read,
            resource: ResourceRecord.UserService,
            data: userListRequestBody
        );

        byte[] requestJson = JsonSerializer.SerializeToUtf8Bytes(readSlotStatusRequest);

        var response = await AccessDaoSystemAsync<List<StoreData>>(requestJson);
        if (response == null)
        {
            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                "Failed to get user list"
            );

            throw new InvalidOperationException("Failed to get user list");
        }
        if (response.IsSuccess)
        {
            if (response.Payload == null)
            {
                await _syslogClient.SyslogAsync(
                    LogLevel.Error,
                    "Failed to get user list: Payload is null"
                );

                throw new InvalidOperationException("Failed to get user list: Payload is null");
            }

            var userList = response.Payload
                .Where(storeData => !string.IsNullOrEmpty(storeData.UserId))
                .Select(storeData => new UserListData(new List<string> { storeData.UserId! }))
                .ToList();

            return userList;
        }
        else
        {
            if (response.Error == null)
            {
                await _syslogClient.SyslogAsync(
                    LogLevel.Error,
                    "Failed to get user list: Unknown error"
                );

                throw new InvalidOperationException("Failed to get user list: Unknown error");
            }

            await _syslogClient.SyslogAsync(
                LogLevel.Error,
                $"Failed to get user list: {response.Error.Message}"
            );

            throw new InvalidOperationException($"Failed to get user list: {response.Error.Message}");
        }
    }

    private async Task<DatabaseResponse<TResponse>?> AccessDaoSystemAsync<TResponse>(byte[] requestJson)
    {
        await _unixDomainSocketClient.ConnectAsync();

        await _unixDomainSocketClient.SendFrameAsync(requestJson);

        byte[]? responseBytes;
        int itemCount;
        (responseBytes, itemCount) = await _unixDomainSocketClient.RecvFrameAsync();
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
