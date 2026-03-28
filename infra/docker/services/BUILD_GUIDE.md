# Docker Build Guide

## Overview

This directory contains Dockerfile definitions and build configurations for Qdrant and SQL Server services.

## Sensitive Data Management

### Environment Variables

Sensitive data (passwords, API keys) are managed through environment variables defined in {ENV_DIR}/.env files:

- {ENV_DIR}/.env.example - Template with placeholders (safe to commit)
- {ENV_DIR}/.env.local - Actual values for local development (gitignored - NOT committed)

### Build Arguments

Dockerfiles accept ARG parameters during build time to pass sensitive data:

```dockerfile
ARG QDRANT_API_KEY=""
ARG SA_PASSWORD="DefaultPassword@123"
```

These are referenced in the Dockerfile and converted to runtime ENV variables.

## Building Images

### Method 1: Using build.sh (Recommended)

Automatically loads environment variables and passes them as build arguments.

Location: {INFRA_HOME}/scripts/build.sh

```bash
cd {INFRA_HOME}/scripts

chmod +x build.sh
./build.sh
```

The script will:
1. Load variables from {ENV_DIR}/.env.local
2. Build Qdrant: {DOCKER_SERVICES_DIR}/qdrant
3. Build SQL Server: {DOCKER_SERVICES_DIR}/sqlserver
4. Tag images appropriately

### Method 2: Manual Build

Build Qdrant:
```bash
export INFRA_HOME=/path/to/infra
export ENV_DIR=${INFRA_HOME}
export COMPOSE_PROJECT_NAME=mgsplus

source ${ENV_DIR}/.env.local

docker build \
    --build-arg QDRANT_API_KEY="${QDRANT_API_KEY}" \
    --build-arg QDRANT_READ_ONLY_API_KEY="${QDRANT_READ_ONLY_API_KEY}" \
    -t ${COMPOSE_PROJECT_NAME}-qdrant:latest \
    ${INFRA_HOME}/docker/services/qdrant
```

Build SQL Server:
```bash
source ${ENV_DIR}/.env.local

docker build \
    --build-arg SA_PASSWORD="${SA_PASSWORD}" \
    --build-arg SQL_ADMIN_USER="${SQL_ADMIN_USER}" \
    --build-arg ACCEPT_EULA="${ACCEPT_EULA}" \
    -t ${COMPOSE_PROJECT_NAME}-sqlserver:latest \
    ${INFRA_HOME}/docker/services/sqlserver
```

### Method 3: Build with docker-compose

Future enhancement when docker-compose.yml is implemented.

## Running Containers

### Run Qdrant

```bash
docker run -d \
    --name qdrant-dev \
    -p 6333:6333 \
    -p 9090:9090 \
    -v qdrant_data:/qdrant/storage \
    mgsplus-qdrant:latest
```

### Run SQL Server

```bash
docker run -d \
    --name sqlserver-dev \
    -e "SA_PASSWORD=YourSuperStrong@Password123" \
    -e "ACCEPT_EULA=Y" \
    -p 1433:1433 \
    -v sqlserver_data:/var/opt/mssql \
    mgsplus-sqlserver:latest
```

## Environment Variable File Setup

1. Copy template to local:
   ```bash
   cp {ENV_DIR}/.env.example {ENV_DIR}/.env.local
   ```

2. Edit with your values:
   ```bash
   nano {ENV_DIR}/.env.local
   ```

3. Never commit {ENV_DIR}/.env.local - it contains sensitive data

## Security Best Practices

Passwords:
- Use strong passwords for SA_PASSWORD (minimum 8 chars: uppercase, lowercase, number, special char)
- Password requirement: MyStr0ng@Pass123 is valid

Configuration:
- Keep {ENV_DIR}/.env.local in {INFRA_HOME}/.gitignore (prevents accidental commit)
- Use build-time ARG for sensitive data instead of hardcoding
- Use runtime ENV variables for passwords in docker run commands

Version Control:
- Never push {ENV_DIR}/.env.local to Git
- Never commit API keys or passwords to any file
- Never store .env.local in version control

## Troubleshooting

### Build fails: permission denied

```bash
chmod +x {INFRA_HOME}/scripts/build.sh
```

### Build fails to load environment file

Verify files exist:
```bash
ls -la {ENV_DIR}/.env.local {ENV_DIR}/.env.example
```

Manual build with environment loaded:
```bash
export ENV_DIR={INFRA_HOME}
source ${ENV_DIR}/.env.local
docker build -t mgsplus-qdrant:latest {INFRA_HOME}/docker/services/qdrant
```

### Container fails to start due to password

Verify SA_PASSWORD in {ENV_DIR}/.env.local meets requirements:
- Minimum 8 characters
- Contains uppercase, lowercase, digit, special character

Check SQL Server logs:
```bash
docker logs sqlserver-dev
```

### Image inspection

List built images:
```bash
docker images | grep mgsplus
```

Inspect image details:
```bash
docker inspect mgsplus-qdrant:latest
docker inspect mgsplus-sqlserver:latest
```

View build history:
```bash
docker history mgsplus-qdrant:latest
docker history mgsplus-sqlserver:latest
```

## File Structure

```
{INFRA_HOME}/
├── .env.example
├── .env.local
├── scripts/
│   └── build.sh
├── docker/
│   ├── services/
│   │   ├── qdrant/
│   │   │   ├── Dockerfile
│   │   │   └── config.yml
│   │   └── sqlserver/
│   │       ├── Dockerfile
│   │       └── README.md
│   └── services/
└── .gitignore
```

## Variable Reference

| Variable | Description |
|----------|-------------|
| {INFRA_HOME} | Infrastructure root directory |
| {ENV_DIR} | Environment configuration directory |
| {DOCKER_SERVICES_DIR} | Docker services directory |
| {COMPOSE_PROJECT_NAME} | Docker project name prefix |

