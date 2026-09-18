/**
 * @file User.cs
 * @brief User record 
 * @author sawada
 * @date 2026-09-15
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType.TableRecord;

[JsonConverter(typeof(JsonStringEnumConverter<UserRoleRecord>))]
public enum UserRoleRecord
{
    [JsonStringEnumMemberName("admin")]
    Admin,
    [JsonStringEnumMemberName("user")]
    User
}

[JsonConverter(typeof(JsonStringEnumConverter<UserStatusRecord>))]
public enum UserStatusRecord
{
    [JsonStringEnumMemberName("active")]
    Active,

    [JsonStringEnumMemberName("inactive")]
    Inactive
}

/**
 * @brief User record
 * @note update and  create
 */
public record UserData (
    [property: JsonPropertyName("user_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UserId = null,

    [property: JsonPropertyName("role")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    UserRoleRecord? Role = null,

    [property: JsonPropertyName("uuid")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UUID = null,

    [property: JsonPropertyName("user_name")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UserName = null,

    [property: JsonPropertyName("user_status")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    UserStatusRecord? UserStatus = null
);
