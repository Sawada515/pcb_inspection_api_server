using api_server.Models.DatabaseType.TableRecord;

namespace api_server.Models
{
    // Desktopアプリケーション
    public record IndividualSlotStatus(string UserName, bool IsStored);
    public record SlotStatusResponse(
        IndividualSlotStatus Slot0,
        IndividualSlotStatus Slot1,
        IndividualSlotStatus Slot2,
        IndividualSlotStatus Slot3,
        IndividualSlotStatus Slot4,
        IndividualSlotStatus Slot5,
        IndividualSlotStatus Slot6,
        IndividualSlotStatus Slot7,
        IndividualSlotStatus Slot8
    );
    public record CreateUserRequest(string Username, string Password);
    public record DeleteUserRequest(string Username);

    public record UserResponse(string Username, DateTime CreatedAt, DateTime? FinalInspectionAt);
    public record UserListResponse(List<UserResponse> Users);

    public record DeviceConnectionStatusResponse(bool IsJetsonConnectedToPlc, bool IsJetsonConnectedToRpi, bool IsPlcConnectedToRobot);

    public record InspectionRequest(string RequestString)
    {
        // 検査開始要求 string -> enum
        public InspectionRequestStatusRecord ToStatus() => RequestString?.ToLowerInvariant() switch
        {
            "waiting"   => InspectionRequestStatusRecord.Waiting,
            "running"   => InspectionRequestStatusRecord.Running,
            "completed" => InspectionRequestStatusRecord.Completed,
            "canceled"  => InspectionRequestStatusRecord.Canceled,
            _ => throw new ArgumentException($"不正なステータス文字列です: '{RequestString}'")
        };
    };

    public record InspectionStateResponse(string Username, string InspectionState);
    public record InspectionStateListResponse(List<InspectionStateResponse> Inspectionsituations);

    public record InspectionHistoryResponse(string Username, DateTime InspectionStartedAt, DateTime InspectionFinishedAt);
    public record InspectionHistoryListResponse(List<InspectionHistoryResponse> InspectionHistories);

    public record InspectionDoorStatusResponse(bool IsDoorOpen);

    // Webアプリケーション
    public record InspectionResponse(int InspectionId, DateTime InspectionAt, int DefectCount);

    public record InspectionEtaResponse(int EtaMinutes);

    public record InspectionResultRequest(string Username);

    // 必要なデータを聞いてから定義する
    //public record DefectResponse()
    //public record InspectionResultResponse()


    // ------------------------------------------------------------------------------------------
    // シングルトン用 オプション
    // ------------------------------------------------------------------------------------------

    public class SyslogClientOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 514;
        public string serviceName { get; set; } = "api_server";
    }

    public class KeycloakOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:8080";
        public string Realm { get; set; } = "master";
        public string ClientId { get; set; } = "api-client";
        public string ClientSecret { get; set; } = "A34T45rVUbaHorGoUQ5OgjUmGomg9BzN";
    }

    public class UnixDomainSocketClientOptions
    {
        public string SocketPath { get; set; } = "/tmp/database_server.sock";
    }
}