# SQL Server 2022 — MGSPlus
# Secrets (SA_PASSWORD) are injected at RUNTIME via docker-compose env_file.
# No secrets baked into the image.
# Running as root to avoid volume permission issues on ARM64/Docker Desktop.
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Install debugging tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    curl \
    netcat-openbsd \
    && rm -rf /var/lib/apt/lists/*

# Prepare data directories
RUN mkdir -p /var/opt/mssql/backup /var/opt/mssql/data \
    && chmod -R 777 /var/opt/mssql

# Runtime env vars (injected by docker-compose via env_file: ../../.env):
#   SA_PASSWORD     — sysadmin password
#   ACCEPT_EULA     — must be "Y"
#   SQLSERVER_DB    — database to create on first start (handled by init script)

EXPOSE 1433

# Healthcheck uses SA_PASSWORD from runtime env
HEALTHCHECK --interval=30s --timeout=15s --start-period=60s --retries=5 \
    CMD /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${SA_PASSWORD}" \
        -C -Q "SELECT 1" -b -o /dev/null || exit 1

CMD ["/opt/mssql/bin/sqlservr"]
