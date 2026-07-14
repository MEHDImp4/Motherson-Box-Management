@echo off
set /p MOTHERSON_DEV_SQL_PASSWORD=Enter the local SQL password: 
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=MothersonBoxDb;User Id=sa;Password=%MOTHERSON_DEV_SQL_PASSWORD%;TrustServerCertificate=True;" --project MothersonBoxManagement
set MOTHERSON_DEV_SQL_PASSWORD=
