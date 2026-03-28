# SQL Server 2022 with monitoring and debugging tools
# Uses official Microsoft SQL Server image as base
FROM mcr.microsoft.com/mssql/server:2022-latest

# Build arguments for sensitive data (pass via --build-arg or runtime ENV)
ARG SA_PASSWORD=""
ARG SQL_ADMIN_USER="sa"
ARG ACCEPT_EULA="N"

# Switch to root to install additional tools
USER root

# Install additional tools for debugging and monitoring
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    git \
    telnet \
    netcat-openbsd \
    && rm -rf /var/lib/apt/lists/*

# Create default database directory
RUN mkdir -p /var/opt/mssql/backup /var/opt/mssql/data

# Grant permissions
RUN chmod 755 /var/opt/mssql/backup /var/opt/mssql/data

# Set environment variables for SQL Server (from args)
ENV SA_PASSWORD=${SA_PASSWORD}
ENV SQL_ADMIN_USER=${SQL_ADMIN_USER}
ENV ACCEPT_EULA=${ACCEPT_EULA}

# Switch back to mssql user for security
USER mssql

# Expose SQL Server port
EXPOSE 1433

# Health check for SQL Server
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1" || exit 1

# Run SQL Server
CMD ["/opt/mssql/bin/sqlservr"]
