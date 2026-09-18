/**
 * @file SyslogClient.cs
 * @brief Syslog client class for sending messages to a syslog server
 * @author sawada
 * @date 2026-09-03
 */

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using api_server.Models;

public enum SyslogFacility
{
    Kernel = 0,
    User = 1,
    Mail = 2,
    Daemon = 3,
    Auth = 4,
    Syslog = 5,
    Lpr = 6,
    News = 7,
    Uucp = 8,
    Cron = 9,
    AuthPriv = 10,
    Ftp = 11,
    Ntp = 12,
    Audit = 13,
    Alert = 14,
    ClockDaemon = 15,
    Local0 = 16,
    Local1 = 17,
    Local2 = 18,
    Local3 = 19,
    Local4 = 20,
    Local5 = 21,
    Local6 = 22,
    Local7 = 23
}

public enum LogLevel
{
    Emergency = 0,
    Alert = 1,
    Critical = 2,
    Error = 3,
    Warning = 4,
    Notice = 5,
    Informational = 6,
    Debug = 7
}

public class SyslogClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;

    private UdpClient? _udpClient;
    private bool _disposed = false;

    private string _serviceName;
    private SyslogFacility? _facility;

    public string ServiceName
    {
        get { return _serviceName ?? "Unknown"; }
        set { _serviceName = value; }
    }

    public SyslogFacility Facility
    {
        get { return _facility ?? SyslogFacility.User; }
        set { _facility = value; }
    }


    public SyslogClient(IOptions<SyslogClientOptions> options)
    {
        var syslogOptions = options.Value;

        _host = syslogOptions.Host;
        _port = syslogOptions.Port;
        _serviceName = syslogOptions.serviceName;

        OpenLog();
    }

    public void OpenLog()
    {
        if (_udpClient == null)
        {
            try
            {
                _udpClient = new UdpClient(_host, _port);
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException("Failed to create UDP client.", ex);
            }
        }
    }

    public async Task SyslogAsync(
        string applicationName,
        SyslogFacility facility,
        LogLevel severity,
        string message)
    {
        int priority = ((int)facility * 8) + (int)severity;

        string timestamp = DateTime.Now.ToString("MMM d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        if (DateTime.Now.Day < 10)
        {
            timestamp = DateTime.Now.ToString("MMM  d HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        string hostname = Environment.MachineName;
        string syslogPayload = $"<{priority}>{timestamp} {hostname} {applicationName}: {message}";

        byte[] sendData = Encoding.UTF8.GetBytes(syslogPayload);

        OpenLog();

        if (_udpClient == null)
        {
            throw new InvalidOperationException("UDP client is not initialized.");
        }

        await _udpClient.SendAsync(sendData, sendData.Length);
    }

    public async Task SyslogAsync(
        LogLevel severity,
        string message)
    {
        await SyslogAsync(_serviceName, Facility, severity, message);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _udpClient?.Dispose();
            _udpClient = null;
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}