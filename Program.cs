using PortSubdomainRouter.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for high performance
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    
    // Listen on port 8443 as specified in traefik config
    options.ListenAnyIP(8443);
});

// Add services
builder.Services.AddSingleton<WebSocketProxyService>();

var app = builder.Build();

// Minimal pipeline - only WebSocket upgrade
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
});

// Single endpoint for WebSocket proxy
app.Map("/", async (HttpContext context, WebSocketProxyService proxyService) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket request required");
        return;
    }

    await proxyService.HandleWebSocketConnection(context);
});

app.Run();
