# Infrastructure Scripts

Central location for infrastructure management scripts. All scripts read configuration from the parent directory ({INFRA_HOME}/).

## Overview

Scripts in this directory automatically load environment variables from:
- {INFRA_HOME}/.env.local (local development - gitignored)
- {INFRA_HOME}/.env.example (template - safe to commit)

This ensures all infrastructure tools (docker, terraform, k8s) use consistent configuration.

## Available Scripts

### build.sh - Build Docker Images

Builds custom Docker images for Qdrant and SQL Server with sensitive data passed as build arguments.

**Usage:**

```bash
cd {INFRA_HOME}/scripts

chmod +x build.sh
./build.sh
```

**What it does:**
1. Loads environment variables from {ENV_DIR}/.env.local
2. Passes sensitive data (API keys, passwords) as Docker build arguments
3. Builds Qdrant image from {INFRA_HOME}/docker/services/qdrant/
4. Builds SQL Server image from {INFRA_HOME}/docker/services/sqlserver/
5. Tags images as: {COMPOSE_PROJECT_NAME}-qdrant:latest and {COMPOSE_PROJECT_NAME}-sqlserver:latest

**Example output:**
```
[INFO] Loading environment from: /path/to/infra/.env.local
[INFO] Building Docker images with build arguments...

[INFO] Building Qdrant image: mgsplus-qdrant:latest
[SUCCESS] Qdrant image built successfully

[INFO] Building SQL Server image: mgsplus-sqlserver:latest
[SUCCESS] SQL Server image built successfully

[SUCCESS] All Docker images built successfully

[INFO] Image summary:
REPOSITORY                TAG       IMAGE ID      CREATED        SIZE
mgsplus-sqlserver         latest    abc123...     2 minutes ago   4.2GB
mgsplus-qdrant            latest    def456...     3 minutes ago   1.2GB
```

**Manual build (single image):**

Build Qdrant only:
```bash
export INFRA_HOME=/path/to/infra
source ${INFRA_HOME}/.env.local

docker build \
    --build-arg QDRANT_API_KEY="${QDRANT_API_KEY}" \
    --build-arg QDRANT_READ_ONLY_API_KEY="${QDRANT_READ_ONLY_API_KEY}" \
    -t ${COMPOSE_PROJECT_NAME}-qdrant:latest \
    ${INFRA_HOME}/docker/services/qdrant
```

Build SQL Server only:
```bash
export INFRA_HOME=/path/to/infra
source ${INFRA_HOME}/.env.local

docker build \
    --build-arg SA_PASSWORD="${SA_PASSWORD}" \
    --build-arg SQL_ADMIN_USER="${SQL_ADMIN_USER}" \
    --build-arg ACCEPT_EULA="${ACCEPT_EULA}" \
    -t ${COMPOSE_PROJECT_NAME}-sqlserver:latest \
    ${INFRA_HOME}/docker/services/sqlserver
```

## Environment Configuration

### Setup

1. Copy template from {INFRA_HOME}/:
   ```bash
   cp {INFRA_HOME}/.env.example {INFRA_HOME}/.env.local
   ```

2. Edit with your values:
   ```bash
   nano {INFRA_HOME}/.env.local
   ```

3. Verify required variables:
   - QDRANT_API_KEY - Qdrant API key (optional)
   - SA_PASSWORD - SQL Server admin password (required, min 8 chars)
   - ACCEPT_EULA=Y - Accept SQL Server license
   - COMPOSE_PROJECT_NAME - Docker image prefix

### Security

- {ENV_DIR}/.env.local is gitignored - safe for sensitive data
- Passwords passed as build arguments (not in Dockerfile)
- Never commit {ENV_DIR}/.env.local
- Never hardcode passwords in Dockerfile or scripts

## File Structure

```
infra/                                (= {INFRA_HOME})
├── .env.example                      (template - commit this)
├── .env.local                        (actual values - gitignored)
├── .gitignore
├── scripts/                          (you are here)
│   ├── build.sh
│   └── README.md
├── docker/
│   ├── services/
│   │   ├── qdrant/
│   │   │   ├── Dockerfile
│   │   │   └── config.yml
│   │   └── sqlserver/
│   │       ├── Dockerfile
│   │       └── README.md
│   ├── init-scripts/
│   └── docs/
├── terraform/                        (future)
└── k8s/                             (future)
```

## Troubleshooting

### Script fails: "No environment file found"

Verify files exist:
```bash
ls -la {INFRA_HOME}/.env.*
```

If missing, create from template:
```bash
cp {INFRA_HOME}/.env.example {INFRA_HOME}/.env.local
```

### Build fails: permission denied

```bash
chmod +x {INFRA_HOME}/scripts/build.sh
cd {INFRA_HOME}/scripts
./build.sh
```

### Docker build fails

Verify Docker is running:
```bash
docker ps
```

Check if service directories exist:
```bash
ls -la {INFRA_HOME}/docker/services/qdrant
ls -la {INFRA_HOME}/docker/services/sqlserver
```

Verify environment variables:
```bash
source {INFRA_HOME}/.env.local
echo "COMPOSE_PROJECT_NAME=${COMPOSE_PROJECT_NAME}"
echo "SA_PASSWORD is set: $([ -n "$SA_PASSWORD" ] && echo 'yes' || echo 'no')"
```

View full build logs:
```bash
docker build -t test:latest {INFRA_HOME}/docker/services/qdrant 2>&1 | tail -50
```

### SQL Server password rejected

Password requirements:
- Minimum 8 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one digit (0-9)
- At least one special character (!@#$%^&*)

Valid example: MyStr0ng@Pass123

## Future Enhancements

Planned scripts:
- run.sh - Start containers with docker-compose
- stop.sh - Stop running containers
- clean.sh - Remove containers, volumes, networks
- logs.sh - View service logs
- terraform-apply.sh - Deploy to cloud (AWS/GCP/Azure)
- k8s-deploy.sh - Deploy to Kubernetes

## Variable Reference

| Variable | Meaning |
|----------|---------|
| {INFRA_HOME} | Infrastructure root directory (infra/) |
| {ENV_DIR} | Environment configuration directory (same as INFRA_HOME) |
| {COMPOSE_PROJECT_NAME} | Docker project name prefix from .env.local |
| {DOCKER_SERVICES_DIR} | Docker services directory (infra/docker/services) |

