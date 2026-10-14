/**
 * @file    Program.cs
 * @brief   API server endpoin
 * @author  sawada 
 * @date    2026-06-29
 */

//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Configuration;

using ApiServer.Services;
using ApiServer.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SyslogClientOptions>(builder.Configuration.GetSection("SyslogClient"));
builder.Services.AddSingleton<SyslogClient>();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("KeycloakClient"));
builder.Services.AddSingleton<ExternalProcessGateway>();

builder.Services.Configure<UnixDomainSocketClientOptions>(builder.Configuration.GetSection("UnixDomainSocketClient"));
builder.Services.AddSingleton<UnixDomainSocketClient>();

WebApplication app = builder.Build();

string? HOST = app.Configuration.GetValue<string>("AppSettings:Host", "localhost");
int PORT = app.Configuration.GetValue<int>("AppSettings:Port", 50000);

/*
 * @brief   機器の接続状態を返す
 */
app.MapGet("/system_status", async (ExternalProcessGateway gateway) => new
{
    system_status = await gateway.GetSystemStatus()
});

/*
 * @brief   アプリケーションログを返す
 */
app.MapGet("/system_log", async (ExternalProcessGateway gateway) =>
{
    string raw_log = await gateway.GetApplicationLog(200);

    return Results.Ok(new
    {
        log = raw_log.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    });
});

/*
 * @brief   基板格納状態を返す
 */
app.MapGet("/slot_status", async (ExternalProcessGateway gateway) => new
{
    slot_status = await gateway.GetSlotStatus()
});


/*
 * @brief   検査完了予定時刻を返す
 */
app.MapGet("/inspection_eta", async (ExternalProcessGateway gateway) => new
{
    eta = "2026-06-29 11:00:00"
});

/*
 * @brief   検査履歴を返す
 */
app.MapGet("/inspection_history", async (ExternalProcessGateway gateway) => new
{
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
 * @brief   検査状況を返す
 */
app.MapGet("/inspection_status", async (ExternalProcessGateway gateway) => new
{
    status = "completed"
});

/*
 * @brief   検査開始要求を送信する
 */
app.MapPost("/inspection_request", async (InspectionRequest request, ExternalProcessGateway gateway) =>
{
    Console.WriteLine("Received inspection request");

    bool success = await gateway.RequestInspection(request);
    if (!success)
    {
        return Results.BadRequest(new { message = "Failed to request inspection" });
    }

    return Results.Ok(new { message = "Inspection requested successfully" });
});


/*
 * @brief   ユーザの一覧を返す
 */
//app.MapGet("/user_list", async (ExternalProcessGateway gateway) => new
//{
//    user_list = await gateway.GetUserList()
//});

/*
 * @brief   ユーザの作成
 */
app.MapPost("/create_user", async (CreateUserRequest request, ExternalProcessGateway gateway) =>
{
    string? uuid = null;

    uuid = await gateway.CreateUser(request);
    if (uuid == null)
    {
        return Results.BadRequest(new { message = "Failed to create user" });
    }

    bool ret = await gateway.UploadUserData(request, uuid);

    return Results.Ok(new { message = $"User created successfully {uuid}" });
});

/*
 * @brief   ユーザの削除
 */
app.MapPost("/delete_user", async (DeleteUserRequest request, ExternalProcessGateway gateway) =>
{
    bool success = await gateway.DeleteUser(request);
    if (!success)
    {
        return Results.BadRequest(new { message = "Failed to delete user" });
    }

    success = await gateway.DeleteUserData(request);
    if (!success)
    {
        return Results.BadRequest(new { message = "Failed to delete user data" });
    }

    return Results.Ok(new { message = "User deleted successfully" });
});


//app.MapGet("/defect_info", (string? user_id) =>
//{
//    // 後で
//    return Results.Ok(new { message = $"Defect info for user {user_id} retrieved successfully" });
//});


app.Run($"http://{HOST}:{PORT}");
