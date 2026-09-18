/**
 * @file Inspection.cs
 * @brief Inspection record 
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType.TableRecord;

public record InspectionData (
    [property: JsonPropertyName("inspection_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? InspectionId = null,

    [property: JsonPropertyName("request_status")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? RequestStatus = null
);
