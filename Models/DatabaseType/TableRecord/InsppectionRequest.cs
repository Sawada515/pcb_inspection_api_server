/**
 * @file InspectionRequest.cs
 * @brief InspectionRequest record
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace api_server.Models.TableRecord;

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
    [property: JsonPropertyName("request_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? RequestId = null,

    [property: JsonPropertyName("bottom_image_path")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? BottomImagePath = null,

    [property: JsonPropertyName("feedback")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Feedback = null,

    [property: JsonPropertyName("user_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UserId = null
);
