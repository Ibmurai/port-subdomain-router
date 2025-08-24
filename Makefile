.PHONY: build run test clean docker-build docker-run docker-stop help

# Variables
IMAGE_NAME = port-subdomain-router
CONTAINER_NAME = port-subdomain-router
PORT = 8443

help: ## Show this help message
	@echo "Port Subdomain Router - Available commands:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

build: ## Build the .NET application
	dotnet build -c Release

run: ## Run the application locally
	dotnet run

test: ## Run tests (port extraction validation)
	python3 test-websocket.py

clean: ## Clean build artifacts
	dotnet clean
	rm -rf bin obj

docker-build: ## Build Docker image
	docker build -t $(IMAGE_NAME) .

docker-run: ## Run Docker container
	docker run -d \
		--name $(CONTAINER_NAME) \
		-p $(PORT):$(PORT) \
		-e TARGET_HOST=host.docker.internal \
		$(IMAGE_NAME)

docker-stop: ## Stop and remove Docker container
	docker stop $(CONTAINER_NAME) || true
	docker rm $(CONTAINER_NAME) || true

docker-logs: ## Show Docker container logs
	docker logs -f $(CONTAINER_NAME)

docker-compose-up: ## Start with docker-compose
	docker-compose up -d

docker-compose-down: ## Stop docker-compose
	docker-compose down

docker-compose-logs: ## Show docker-compose logs
	docker-compose logs -f

publish: ## Publish single file for production
	dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

size: ## Show Docker image size
	docker images $(IMAGE_NAME)

all: clean build docker-build ## Clean, build, and create Docker image

# CI/CD commands
ci-test: ## Run CI tests locally
	dotnet restore
	dotnet build --configuration Release
	python3 test-websocket.py

ci-build: ## Run CI build locally
	docker build -t $(IMAGE_NAME):ci .

ci-security: ## Run security scan locally
	docker run --rm -v $(PWD):/workspace aquasec/trivy fs /workspace

release-prep: ## Prepare for release
	@echo "Preparing for release..."
	@echo "1. Ensure all tests pass: make ci-test"
	@echo "2. Build image: make ci-build"
	@echo "3. Run security scan: make ci-security"
	@echo "4. Create GitHub release with tag (e.g., v1.0.0)"
	@echo "5. CI/CD will automatically publish to ghcr.io"
