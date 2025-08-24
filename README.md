# Port Subdomain Router

A high-performance C# 9 WebSocket proxy that routes traffic based on subdomain ports. The application extracts the port number from the subdomain (e.g., `12345.vnc.example.com` → port 12345) and proxies WebSocket traffic to the target host on that port.

## Features

- **Minimal & Fast**: Built with .NET 9 and Kestrel for maximum performance
- **Memory Efficient**: Uses pooled buffers and pipelines to minimize allocations
- **Binary WebSocket Support**: Optimized for binary frames without UTF-8 validation
- **Backpressure Handling**: Proper flow control with await-based sends
- **Connection Limits**: Configurable timeouts and connection limits
- **Containerized**: Ready-to-deploy Docker image

## Architecture

The application works as follows:

1. **Subdomain Parsing**: Extracts port from subdomain using regex pattern
2. **Port Validation**: Ensures port is in range 10000-42000
3. **TCP Connection**: Establishes connection to target host on extracted port
4. **Bidirectional Bridge**: Efficiently bridges WebSocket ↔ TCP traffic
5. **Error Handling**: Graceful handling of connection failures and timeouts

## Configuration

### Environment Variables

- `TARGET_HOST`: Target host to proxy to (default: `localhost`)
- `ASPNETCORE_ENVIRONMENT`: Environment setting (default: `Production`)

### Port Range

The application accepts ports in the range **10000-42000** as specified in the regex pattern:
- `10000-39999`: `[1-3][0-9]{4}`
- `40000-41999`: `40[0-9]{3}`
- `41000-41999`: `41[0-9]{3}`
- `42000`: `42000`

## Deployment

### Docker Compose

```bash
# Build and run
docker-compose up -d

# View logs
docker-compose logs -f port-subdomain-router
```

### Docker

```bash
# Build image
docker build -t port-subdomain-router .

# Run container
docker run -d \
  --name port-subdomain-router \
  -p 8443:8443 \
  -e TARGET_HOST=your-target-host \
  port-subdomain-router
```

### Kubernetes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: port-subdomain-router
spec:
  replicas: 1
  selector:
    matchLabels:
      app: port-subdomain-router
  template:
    metadata:
      labels:
        app: port-subdomain-router
    spec:
      containers:
      - name: port-subdomain-router
        image: port-subdomain-router:latest
        ports:
        - containerPort: 8443
        env:
        - name: TARGET_HOST
          value: "your-target-host"
        resources:
          requests:
            memory: "64Mi"
            cpu: "50m"
          limits:
            memory: "128Mi"
            cpu: "200m"
```

## Traefik Integration

The application is designed to work with Traefik using the following labels:

```yaml
- traefik.http.routers.vnc-ws.rule=HostRegexp(`([1-3][0-9]{4}|40[0-9]{3}|41[0-9]{3}|42000)\.vnc\.${ROOT_DOMAIN}`)
- traefik.http.routers.vnc-ws.entrypoints=websecure
- traefik.http.routers.vnc-ws.tls=true
- traefik.http.routers.vnc-ws.tls.certresolver=le
- traefik.http.routers.vnc-ws.tls.domains[0].main=*.vnc.${ROOT_DOMAIN}
- traefik.http.routers.vnc-ws.service=the-app
- traefik.http.services.the-app.loadbalancer.server.port=8443
- traefik.http.services.the-app.loadbalancer.passhostheader=true
```

## Performance Optimizations

- **Pooled Buffers**: Uses `ArrayPool<byte>` for efficient memory management
- **Minimal Middleware**: Only WebSocket upgrade, no full ASP.NET stack
- **Binary Frames**: No UTF-8 validation overhead
- **Connection Limits**: Configurable limits to prevent resource exhaustion
- **Timeouts**: Proper idle and connection timeouts
- **Single File**: Self-contained deployment for minimal container size

## Monitoring

The application includes:
- Structured logging with correlation IDs
- Health check endpoint
- Connection metrics and error tracking
- Graceful shutdown handling

## Security

- Non-root container user
- Minimal attack surface (single endpoint)
- Input validation and sanitization
- Connection timeouts to prevent DoS
- No TLS termination (handled by Traefik)

## Development

### Prerequisites

- .NET 9 SDK
- Docker (for containerized builds)

### Local Development

```bash
# Restore dependencies
dotnet restore

# Run locally
dotnet run

# Build for production
dotnet publish -c Release
```

### Testing

The application can be tested with any WebSocket client connecting to a valid subdomain port:

```javascript
// Example WebSocket connection
const ws = new WebSocket('wss://12345.vnc.example.com');
```

## License

MIT License
