/**
 * @file Inspection.cs
 * @brief Inspection record 
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace ApiServer.Models.DatabaseType.TableRecord;

public record InspectionData (
    [property: JsonPropertyName("inspection_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? InspectionId = null,

    [property: JsonPropertyName("started_at")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTime? StartedAt = null,

    [property: JsonPropertyName("finished_at")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTime? FinishedAt = null,

    [property: JsonPropertyName("top_image_path")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? TopImagePath = null,

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
