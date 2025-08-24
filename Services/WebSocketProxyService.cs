using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.RegularExpressions;

namespace PortSubdomainRouter.Services;

public class WebSocketProxyService
{
    private static readonly Regex PortRegex = new(@"^([1-3][0-9]{4}|40[0-9]{3}|41[0-9]{3}|42000)\.", RegexOptions.Compiled);
    private static readonly string TargetHost = Environment.GetEnvironmentVariable("TARGET_HOST") ?? "localhost";
    private static readonly int ConnectionTimeoutMs = 5000;
    private static readonly int IdleTimeoutMs = 300000; // 5 minutes
    
    private readonly ILogger<WebSocketProxyService> _logger;

    public WebSocketProxyService(ILogger<WebSocketProxyService> logger)
    {
        _logger = logger;
    }

    public async Task HandleWebSocketConnection(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var port = ExtractPortFromHost(host);
        
        if (port == null)
        {
            _logger.LogWarning("Invalid host format: {Host}", host);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid subdomain format");
            return;
        }

        _logger.LogInformation("Proxying WebSocket connection to {TargetHost}:{Port}", TargetHost, port);

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        using var cts = new CancellationTokenSource(IdleTimeoutMs);
        
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(TargetHost, port.Value);
            
            if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
            {
                _logger.LogError("Connection timeout to {TargetHost}:{Port}", TargetHost, port);
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Connection timeout", CancellationToken.None);
                return;
            }

            await connectTask; // Re-await to propagate any exceptions
            
            using var tcpStream = tcpClient.GetStream();
            
            // Create bidirectional bridge with backpressure
            var webSocketToTcp = BridgeWebSocketToTcp(webSocket, tcpStream, cts.Token);
            var tcpToWebSocket = BridgeTcpToWebSocket(tcpStream, webSocket, cts.Token);
            
            await Task.WhenAny(webSocketToTcp, tcpToWebSocket);
            
            _logger.LogInformation("WebSocket proxy session ended for {TargetHost}:{Port}", TargetHost, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket proxy for {TargetHost}:{Port}", TargetHost, port);
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Proxy error", CancellationToken.None);
            }
        }
    }

    private static int? ExtractPortFromHost(string host)
    {
        var match = PortRegex.Match(host);
        if (!match.Success) return null;
        
        if (int.TryParse(match.Groups[1].Value, out var port))
        {
            return port >= 10000 && port <= 42000 ? port : null;
        }
        
        return null;
    }

    private static async Task BridgeWebSocketToTcp(WebSocket webSocket, Stream tcpStream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
                    break;
                }
                
                if (result.Count > 0)
                {
                    await tcpStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                    await tcpStream.FlushAsync(cancellationToken);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task BridgeTcpToWebSocket(Stream tcpStream, WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await tcpStream.ReadAsync(buffer, cancellationToken);
                
                if (bytesRead == 0) // TCP connection closed
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Target closed", CancellationToken.None);
                    break;
                }
                
                await webSocket.SendAsync(buffer.AsMemory(0, bytesRead), WebSocketMessageType.Binary, false, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
