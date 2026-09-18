/**
 * @file Store.cs
 * @brief Store record 
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType.TableRecord;

[JsonConverter(typeof(JsonStringEnumConverter<StoreStatusRecord>))]
public enum StoreStatusRecord
{
    [JsonStringEnumMemberName("empty")]
    Empty = 0,
    [JsonStringEnumMemberName("stored")]
    Stored = 1,
    [JsonStringEnumMemberName("checking")]
    Checking = 2,
    [JsonStringEnumMemberName("checked")]
    Checked = 3
}

/**
 * @brief Store record
 * @note update and read
 * @warn when update, all fields must be specified. If you want to update only one field, you need to read the record first and then update it.
 */
public record StoreData (
    [property: JsonPropertyName("store_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? StoreId = null,

    [property: JsonPropertyName("col")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? Col = null,

    [property: JsonPropertyName("row")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? Row = null,

    [property: JsonPropertyName("store_status")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    StoreStatusRecord? StoreStatus = null,

    [property: JsonPropertyName("user_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? UserId = null
);
