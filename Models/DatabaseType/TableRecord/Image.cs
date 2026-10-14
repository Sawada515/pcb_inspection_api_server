/**
 * @file Image.cs
 * @brief Image record 
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace ApiServer.Models.DatabaseType.TableRecord;

public record ImageData (
    [property: JsonPropertyName("image_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? ImageId = null,

    [property: JsonPropertyName("defect_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? DefectId = null,

    [property: JsonPropertyName("DefectImagePath")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DefectImagePath = null
);
