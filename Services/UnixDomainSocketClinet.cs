/**
 * @file UnixDomainSocketClient.cs
 * @brief UnixDomainSocketClient class for managing Unix domain socket client
 * @author sawada
 * @date 2026-09-03
 */

using System.Buffers.Binary;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

using api_server.Models;

public class UnixDomainSocketClient : IDisposable
{
    private readonly string _socketPath;
    private Socket? _socket;
    private NetworkStream? _stream;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _recvLock = new(1, 1);
    private bool _disposed = false;
    private const int MaxPayloadSize = 2 * 1024; // 2kB
    private const int MAX_RETRY_COUNT = 5;

    /**
     * @brief コンストラクタ
     * @param[in] socketPath Unixドメインソケットのパス
     */
    public UnixDomainSocketClient(IOptions<UnixDomainSocketClientOptions> options)
    {
        _socketPath = options.Value.SocketPath;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_socket is { Connected: true })
        {
            return;
        }

        // 接続処理は送受両方のロックを取得して安全に行う
        await _sendLock.WaitAsync(cancellationToken);
        await _recvLock.WaitAsync(cancellationToken);
        try
        {
            if (_socket is { Connected: true })
            {
                return;
            }

            CleanUp();

            _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(_socketPath);

            await _socket.ConnectAsync(endpoint, cancellationToken);
            _stream = new NetworkStream(_socket, ownsSocket: true);
        }
        finally
        {
            _recvLock.Release();
            _sendLock.Release();
        }
    }

    /**
     * @brief バイト列をそのまま生データとして送信する
     */
    public async Task<bool> SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Socket is not connected.");
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(data, cancellationToken);

            return true;
        }
        catch (SocketException ex)
        {
            CleanUp();

            Console.WriteLine($"[UnixDomainSocket] SocketException: {ex.Message}");

            throw;
        }
        catch (IOException ex)
        {
            CleanUp();

            Console.WriteLine($"[UnixDomainSocket] IOException: {ex.Message}");

            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /**
     * @brief 4バイトの電文長ヘッダーを付与して送信する
     */
    public async Task<bool> SendFrameAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        byte[] frame = new byte[sizeof(int) + payload.Length];

        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, sizeof(int)), payload.Length);

        payload.Span.CopyTo(frame.AsSpan(sizeof(int)));

        bool ret;

        try
        {
            ret =await SendAsync(frame, cancellationToken);
        }
        catch (IOException)
        {
            ret = await RetryWithSendAsync(frame, cancellationToken);
        }
        catch (SocketException)
        {
            ret = await RetryWithSendAsync(frame, cancellationToken);
        }

        return ret;
    }

    private async Task<bool> RetryWithSendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        bool ret;

        await ConnectAsync(cancellationToken);

        for (int retryCount = 0; retryCount < MAX_RETRY_COUNT; ++retryCount)
        {
            try
            {
                ret = await SendAsync(frame, cancellationToken);
                if (ret)
                {
                    return true;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[UnixDomainSocket] IOException on retry {retryCount + 1}: {ex.Message}");

                await ConnectAsync(cancellationToken);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[UnixDomainSocket] SocketException on retry {retryCount + 1}: {ex.Message}");

                await ConnectAsync(cancellationToken);
            }
        }

        return false;
    }

    /**
     * @brief 指定したバイト数のデータを受信する
     */
    public async Task<byte[]?> ReceiveAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return Array.Empty<byte>();
        }

        byte[] buffer = new byte[count];
        bool success = await ReadExactAsync(buffer, cancellationToken);

        return success ? buffer : null;
    }

    /**
     * @brief 4バイトの電文長ヘッダーを読み取り、ペイロードを受信する
     */
    public async Task<byte[]?> RecvFrameAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Socket is not connected.");
        }
    
        await _recvLock.WaitAsync(cancellationToken);
    
        try
        {
            byte[] header = new byte[sizeof(int)];
    
            if (!await ReadExactAsync(header, cancellationToken))
            {
                return null;
            }
    
            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
    
            if (payloadLength < 0 || payloadLength > MaxPayloadSize)
            {
                Console.WriteLine($"[UnixDomainSocket] Invalid payload length: {payloadLength}");
    
                CleanUp();

                return null;
            }
    
            if (payloadLength == 0)
            {
                return Array.Empty<byte>();
            }
    
            byte[] payload = new byte[payloadLength];
    
            if (!await ReadExactAsync(payload, cancellationToken))
            {
                return null;
            }
    
            return payload;
        }
        finally
        {
            _recvLock.Release();
        }
    }

    /**
     * @brief 指定したバッファを満たすまで NetworkStream から確実に読み切る
     */
    private async Task<bool> ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Socket is not connected.");
        }

        try
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = await _stream.ReadAsync(buffer.Slice(totalRead), cancellationToken);
                if (read == 0)
                {
                    Console.WriteLine("[UnixDomainSocket] Remote peer closed connection.");

                    CleanUp();

                    return false;
                }
                
                totalRead += read;
            }

            return true;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[UnixDomainSocket] Error reading data: {ex.Message}");

            CleanUp();

            throw;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[UnixDomainSocket] SocketException: {ex.Message}");

            CleanUp();

            throw;
        }
    }

    private void CleanUp()
    {
        _stream?.Dispose();
        _socket?.Dispose();

        _stream = null;
        _socket = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CleanUp();
        _sendLock.Dispose();
        _recvLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
