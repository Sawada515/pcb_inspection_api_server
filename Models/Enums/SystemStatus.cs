/**
 * @file    SystemStatus.cs
 * @brief   system status enum
 * @author  sawada 
 * @date    2026-09-01
 */

namespace ApiServer.Models.Enums
{
    public enum SystemStatus
    {
        /**
         * @brief 正常
         */
        normal = 0,
        /**
         * @brief 接続異常
         */
        not_connected = 1,
    }

    public static class SystemStatusExtensions
    {
        public static string ToString(this SystemStatus status) => status switch
        {
            SystemStatus.normal => "正常",
            SystemStatus.not_connected => "接続異常",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}