/**
 * @file Response.cs
 * @brief Response class for managing database response types
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace ApiServer.Models.DatabaseType;

[JsonConverter(typeof(JsonStringEnumConverter<ResponseStatus>))]
public enum ResponseStatus
{
    [JsonStringEnumMemberName("success")]
    Success,

    [JsonStringEnumMemberName("error")]
    Error
}

public record DatabaseError (
    [property: JsonPropertyName("error_code")]
    int? ErrorCode = null,

    [property: JsonPropertyName("message")]
    string? Message = null
);

public record DatabaseResponse<TPayload> (
    [property: JsonPropertyName("request_id")]
    int RequestId,

    [property: JsonPropertyName("status")]
    ResponseStatus Status,

    [property: JsonPropertyName("payload")]
    TPayload? Payload,

    [property: JsonPropertyName("error")]
    DatabaseError? Error = null
)
{
    public bool IsSuccess => Status == ResponseStatus.Success && Error == null;
}
