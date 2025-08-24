# Port Subdomain Router v{{VERSION}}

## 🚀 What's New

<!-- Describe the key features and improvements in this release -->

## 🔧 Changes

### Added
- <!-- New features -->

### Changed
- <!-- Changes in existing functionality -->

### Fixed
- <!-- Bug fixes -->

### Security
- <!-- Security improvements -->

## 📦 Installation

### Docker

```bash
# Pull the latest release
docker pull ghcr.io/{{OWNER}}/port-subdomain-router:{{VERSION}}

# Run the container
docker run -d \
  --name port-subdomain-router \
  -p 8443:8443 \
  -e TARGET_HOST=your-target-host \
  ghcr.io/{{OWNER}}/port-subdomain-router:{{VERSION}}
```

### Docker Compose

```yaml
version: '3.8'
services:
  port-subdomain-router:
    image: ghcr.io/{{OWNER}}/port-subdomain-router:{{VERSION}}
    ports:
      - "8443:8443"
    environment:
      - TARGET_HOST=your-target-host
```

## 🔗 Links

- [Documentation](https://github.com/{{OWNER}}/port-subdomain-router#readme)
- [Docker Hub](https://ghcr.io/{{OWNER}}/port-subdomain-router)
- [Issues](https://github.com/{{OWNER}}/port-subdomain-router/issues)

## 📋 Changelog

<!-- Include a link to the full changelog or list key changes -->

## 🔒 Security

This release includes the following security improvements:
- <!-- List security updates -->

## 📊 Metrics

- Image size: <!-- Size in MB -->
- Build time: <!-- Time in minutes -->
- Test coverage: <!-- Percentage if available -->

---

**Note**: This release is automatically published to GitHub Container Registry (ghcr.io) when the release is created.
