# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files
COPY *.csproj ./
RUN dotnet restore --runtime linux-x64

# Copy source code
COPY . ./

# Build optimized single file
RUN dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine
WORKDIR /app

# Install runtime dependencies
RUN apk add --no-cache icu-libs

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -D -s /bin/sh -u 1000 -G appuser appuser

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8443/ || exit 1

# Run the application
ENTRYPOINT ["./PortSubdomainRouter"]
