/**
 * @file Types.cs
 * @brief APIサーバーの型定義
 * @author Sawada
 * @date 2024-06-15
 */

using System.Text.Json.Serialization;

using ApiServer.Models.DatabaseType.TableRecord;

namespace ApiServer.Models
{
    // Desktopアプリケーション
    public record IndividualSlotStatus(
        [property: JsonPropertyName("username")]
        string UserName,
        [property: JsonPropertyName("is_stored")]
        bool IsStored
    );
    public record SlotStatusResponse(
        [property: JsonPropertyName("slot0")]
        IndividualSlotStatus Slot0,
        [property: JsonPropertyName("slot1")]
        IndividualSlotStatus Slot1,
        [property: JsonPropertyName("slot2")]
        IndividualSlotStatus Slot2,
        [property: JsonPropertyName("slot3")]
        IndividualSlotStatus Slot3,
        [property: JsonPropertyName("slot4")]
        IndividualSlotStatus Slot4,
        [property: JsonPropertyName("slot5")]
        IndividualSlotStatus Slot5,
        [property: JsonPropertyName("slot6")]
        IndividualSlotStatus Slot6,
        [property: JsonPropertyName("slot7")]
        IndividualSlotStatus Slot7,
        [property: JsonPropertyName("slot8")]
        IndividualSlotStatus Slot8
    );
    public record CreateUserRequest(
        [property: JsonPropertyName ("username")]
        string Username,
        [property: JsonPropertyName ("password")]
        string Password,
        [property: JsonPropertyName ("col")]
        int Col,
        [property: JsonPropertyName ("row")]
        int Row
     );
    public record DeleteUserRequest(
        [property: JsonPropertyName("username")]
        string Username
    );

    public record UserResponse(
        [property: JsonPropertyName("username")]
        string Username,
        [property: JsonPropertyName("created_at")]
        DateTime CreatedAt,
        [property: JsonPropertyName("final_inspection_at")]
        DateTime? FinalInspectionAt
    );
    public record UserListResponse(
        [property: JsonPropertyName("users")]
        List<UserResponse> Users
    );

    public record DeviceConnectionStatusResponse(
        [property: JsonPropertyName("is_jetson_connected_to_plc")]
        bool IsJetsonConnectedToPlc,
        [property: JsonPropertyName("is_jetson_connected_to_rpi")]
        bool IsJetsonConnectedToRpi,
        [property: JsonPropertyName("is_plc_connected_to_robot")]
        bool IsPlcConnectedToRobot
    );

    public record InspectionRequest(
        [property: JsonPropertyName("request_string")]
        string RequestString
    )
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

    public record InspectionStateResponse(
        [property: JsonPropertyName("username")]
        string Username,
        [property: JsonPropertyName("inspection_state")]
        string InspectionState
    );
    public record InspectionStateListResponse(
        [property: JsonPropertyName("inspectionsituations")]
        List<InspectionStateResponse> Inspectionsituations
    );

    public record InspectionHistoryResponse(
        [property: JsonPropertyName("username")]
        string Username,
        [property: JsonPropertyName("inspection_started_at")]
        DateTime InspectionStartedAt,
        [property: JsonPropertyName("inspection_finished_at")]
        DateTime InspectionFinishedAt
    );
    public record InspectionHistoryListResponse(
        [property: JsonPropertyName("inspection_histories")]
        List<InspectionHistoryResponse> InspectionHistories
    );

    public record InspectionDoorStatusResponse(
        [property: JsonPropertyName("is_door_open")]
        bool IsDoorOpen
    );

    // Webアプリケーション
    public record InspectionResponse(
        [property: JsonPropertyName("inspection_id")]
        int InspectionId,
        [property: JsonPropertyName("inspection_at")]
        DateTime InspectionAt,
        [property: JsonPropertyName("defect_count")]
        int DefectCount
    );

    public record InspectionEtaResponse(
        [property: JsonPropertyName("eta_minutes")]
        int EtaMinutes
    );

    public record InspectionResultRequest(
        [property: JsonPropertyName("username")]
        string Username
    );

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
        public string ServiceName { get; set; } = "ApiServer";
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