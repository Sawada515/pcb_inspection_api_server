/**
 * @file Defect.cs
 * @brief Defect record 
 * @author sawada
 * @date 2026-09-16
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType.TableRecord;

[JsonConverter(typeof(JsonStringEnumConverter<BoardSideRecord>))]
public enum BoardSideRecord
{
    [JsonStringEnumMemberName("top")]
    Top,
    [JsonStringEnumMemberName("bottom")]
    Bottom
}

public enum DefectTypeRecord
{
    NoSolder,
    ExcessSolder,
    InsufficientSolder,
    ExposedCopper,
    PoorWetting,
    SolderBall,
    SolderBridge,
    ExcessiveLeadProtrusion,
    InsufficientLeadProtrusion,
    ReversedPolarity,
    ReversedOrientation,
    ComponentMisalignment,
    LiftedComponent,
    LiftedTrace

}

public class DefectTypeRecordString {
    public static readonly string[] Name =
    [
        "未はんだ",
        "はんだ過剰",
        "はんだ不足",
        "はんだ赤目",
        "はんだ濡れ不足",
        "はんだボール",
        "ブリッジ",
        "リード線突き出し大",
        "リード線突き出し小",
        "取付方向逆",
        "極性逆",
        "実装の位置ずれ",
        "部品の浮き上がり",
        "パターンの剥離"
    ];
}

public record DefectData (
    [property: JsonPropertyName("defect_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? DefectId = null,

    [property: JsonPropertyName("board_side")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    BoardSideRecord? BoardSide = null,

    [property: JsonPropertyName("point")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    List<int>? Point = null,

    [property: JsonPropertyName("area")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    float? Area = null,

    [property: JsonPropertyName("defect_type_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? DefectTypeId = null,

    [property: JsonPropertyName("inspection_id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? InspectionId = null
);
