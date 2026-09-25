/**
 * @file Request.cs
 * @brief Request class for managing database request types
 * @author sawada
 * @date 2026-09-10
 */

using System.Text.Json.Serialization;

namespace api_server.Models.DatabaseType;

[JsonConverter(typeof(JsonStringEnumConverter<QueryTypeRecord>))]
public enum QueryTypeRecord
{
    [JsonStringEnumMemberName("create")]
    Create,
    [JsonStringEnumMemberName("read")]
    Read,
    [JsonStringEnumMemberName("update")]
    Update,
    [JsonStringEnumMemberName("delete")]
    Delete
}

[JsonConverter(typeof(JsonStringEnumConverter<ResourceRecord>))]
public enum ResourceRecord
{
    [JsonStringEnumMemberName("user_service")]
    UserService,
    [JsonStringEnumMemberName("inspection_service")]
    InspectionService,
    [JsonStringEnumMemberName("request_service")]
    InspectionRequestService,
    [JsonStringEnumMemberName("store_service")]
    StoreService,
    [JsonStringEnumMemberName("defect_service")]
    DefectService,
    [JsonStringEnumMemberName("defect_type_service")]
    DefectTypeService,
    [JsonStringEnumMemberName("image_service")]
    ImageService
}

public record DatabasePayload<TData>
(
    [property: JsonPropertyName("query_type")]
    QueryTypeRecord QueryType,

    [property: JsonPropertyName("resource")]
    ResourceRecord Resource,

    [property: JsonPropertyName("data")]
    TData Data
);

public record DatabaseRequest<TData>
(
    [property: JsonPropertyName("request_id")]
    int RequestId,

    [property: JsonPropertyName("payload")]
    DatabasePayload<TData> Payload
)
{
    public DatabaseRequest(int requestId, QueryTypeRecord queryType, ResourceRecord resource, TData data)
        : this(requestId, new DatabasePayload<TData>(queryType, resource, data)) {}
}
