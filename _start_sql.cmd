@echo off
set /p MOTHERSON_DEV_SQL_PASSWORD=Enter the local SQL password: 
docker run -d --name motherson-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=%MOTHERSON_DEV_SQL_PASSWORD%" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04
set MOTHERSON_DEV_SQL_PASSWORD=
