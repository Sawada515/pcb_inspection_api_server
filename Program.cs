/**
 * @file    Program.cs
 * @brief   API server endpoin
 * @author  sawada 
 * @date    2026-06-29
 */

using api_server.Services;
using api_server.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SyslogClientOptions>(builder.Configuration.GetSection("SyslogClient"));
builder.Services.AddSingleton<SyslogClient>();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.AddSingleton<ExternalProcessGateway>();

builder.Services.Configure<UnixDomainSocketClientOptions>(builder.Configuration.GetSection("UnixDomainSocketClient"));
builder.Services.AddSingleton<UnixDomainSocketClient>();

var app = builder.Build();

string? HOST = app.Configuration.GetValue<string>("AppSettings:Host", "localhost");
int PORT = app.Configuration.GetValue<int>("AppSettings:Port", 50000);

/*
 * @brief   API server endpoint system_status
 */
app.MapGet("/system_status", async (ExternalProcessGateway gateway) => new
{
    system_status = await gateway.GetSystemStatus()
});

/*
 * @brief   API server endpoint system_log
 */
app.MapGet("/system_log", async (ExternalProcessGateway gateway) =>
{
    string raw_log = await gateway.GetApplicationLog(100);

    return Results.Ok(new
    {
        log = raw_log.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    });
});

/*
 * @brief   API server endpoint slot_statu
 */
app.MapGet("/slot_status", async (ExternalProcessGateway gateway) => new
{
    slot_status = await gateway.GetSlotStatus()
});

//app.MapPost("/slot_status", ())

/*
 * @brief   API server endpoint slot_subscribe_info
 */
app.MapGet("/slot_subscribe_info", (ExternalProcessGateway gateway) => new
{
    info = gateway.GetSlotStatus()
});

/*
 * @brief   API server endpoint inspection_eta
 */
app.MapGet("/inspection_eta", async (ExternalProcessGateway gateway) => new {
    eta = "2026-06-29 11:00:00"
});

/*
 * @brief   API server endpoint inspection_history
 */
app.MapGet("/inspection_history", async (ExternalProcessGateway gateway) => new {
    history = new List<string> {
        "2026-06-29 10:00:00 - Inspection started",
        "2026-06-29 10:05:00 - Inspection completed",
        "2026-06-29 10:10:00 - Inspection started",
        "2026-06-29 10:15:00 - Inspection completed",
        "2026-06-29 10:20:00 - Inspection started",
        "2026-06-29 10:25:00 - Inspection completed"
    }
});

/*
 * @brief   API server endpoint inspection_status
 */
app.MapGet("/inspection_status", async (ExternalProcessGateway gateway) => new {
    status = "completed"
});

/*
 * @brief   API server endpoint user
 */
app.MapPost("/create_user", async (CreateUserData user, ExternalProcessGateway gateway) => {
    string? uuid = await gateway.CreateUser(user);
    if (uuid == null)
    {
        return Results.BadRequest(new { message = "Failed to create user" });
    }

    return Results.Ok(new { message = $"User created successfully {uuid}" });
});

app.MapPost("/delete_user", async (DeleteUserData user, ExternalProcessGateway gateway) => {
    bool success = await gateway.DeleteUser(user);
    if (!success)
    {
        return Results.BadRequest(new { message = "Failed to delete user" });
    }

    return Results.Ok(new { message = "User deleted successfully" });
});

/*
 * @brief   API server endpoint inspection_request
 */
//app.MapPost("/inspection_request", (InspectionRequest request) => {
//    // 後で
//    return Results.Ok(new { message = "Inspection request received successfully" });
//});

app.MapGet("/defect_info", (string? user_id) => {
    // 後で
    return Results.Ok(new { message = $"Defect info for user {user_id} retrieved successfully" });
});


app.Run($"http://{HOST}:{PORT}");
