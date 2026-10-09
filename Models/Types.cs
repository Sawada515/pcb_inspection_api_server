using api_server.Models.DatabaseType.TableRecord;

namespace api_server.Models
{
    public record IndividualSlotStatusData(string UserName, bool IsStored);
    public record SlotStatusData(
        IndividualSlotStatusData Slot0,
        IndividualSlotStatusData Slot1,
        IndividualSlotStatusData Slot2,
        IndividualSlotStatusData Slot3,
        IndividualSlotStatusData Slot4,
        IndividualSlotStatusData Slot5,
        IndividualSlotStatusData Slot6,
        IndividualSlotStatusData Slot7,
        IndividualSlotStatusData Slot8
    );
    public record CreateUserData(string Username, string Password);
    public record DeleteUserData(string Username);

    public record UserListData(string Username, DateTime CreatedAt, DateTime FinalInspection);

    public record InspectionRequestStringData(string RequestString)
    {
        public InspectionRequestStatusRecord ToStatus() => RequestString?.ToLowerInvariant() switch
        {
            "waiting"   => InspectionRequestStatusRecord.Waiting,
            "running"   => InspectionRequestStatusRecord.Running,
            "completed" => InspectionRequestStatusRecord.Completed,
            "canceled"  => InspectionRequestStatusRecord.Canceled,
            _ => throw new ArgumentException($"不正なステータス文字列です: '{RequestString}'")
        };
    };

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