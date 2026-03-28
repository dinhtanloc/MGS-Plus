# SQL Server Service

## Overview
Uses official Microsoft SQL Server 2022 image with additional monitoring and debugging tools.

## Base Image
```
mcr.microsoft.com/mssql/server:2022-latest
```

## Additional Tools
- wget, curl: Network utilities
- git: Version control
- telnet, netcat: Network debugging

## Environment Variables Required
Set in docker-compose.yml or .env:
- `SA_PASSWORD`: SQL Server Administrator password (minimum 8 characters, uppercase, lowercase, number, special char)
- `ACCEPT_EULA`: Must be set to "Y" to accept SQL Server license

## Volumes
- `/var/opt/mssql/data`: Database files
- `/var/opt/mssql/backup`: Backup files
- `/var/opt/mssql/log`: Log files

## Port
- `1433`: Default SQL Server port

## Health Check
Runs `sqlcmd` query to verify SQL Server is responsive.

## Custom Initialization
1. Place `.sql` files in `init-scripts/sqlserver/`
2. Reference in docker-compose.yml to auto-execute on startup

## Example Connection String
```
Server=localhost,1433;Database=master;User Id=sa;Password=<SA_PASSWORD>;
```
