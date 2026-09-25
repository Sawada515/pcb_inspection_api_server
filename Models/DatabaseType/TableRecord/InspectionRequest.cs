/**
 * @file InspectionRequest.cs
 * @brief InspectionRequest record
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType.TableRecord;

[JsonConverter(typeof(JsonStringEnumConverter<InspectionRequestStatusRecord>))]
public enum InspectionRequestStatusRecord
{
    [JsonStringEnumMemberName("waiting")]
    Waiting,
    
    [JsonStringEnumMemberName("running")]
    Running,

    [JsonStringEnumMemberName("completed")]
    Completed,

    [JsonStringEnumMemberName("canceled")]
    Canceled
}

public record InspectionRequestData (
    [property: JsonPropertyName("inspection_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? InspectionId = null,

    [property: JsonPropertyName("request_status")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    InspectionRequestStatusRecord? RequestStatus = null
);

