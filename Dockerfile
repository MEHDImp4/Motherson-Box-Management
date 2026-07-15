FROM mcr.microsoft.com/dotnet/sdk:8.0.422 AS build
WORKDIR /src

COPY Motherson_Box_Management.sln ./
COPY MothersonBoxManagement/MothersonBoxManagement.csproj MothersonBoxManagement/
COPY MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj MothersonBoxManagement.Tests/
COPY MothersonBoxManagement.PrintAgent.Core/MothersonBoxManagement.PrintAgent.Core.csproj MothersonBoxManagement.PrintAgent.Core/
COPY MothersonBoxManagement.PrintAgent/MothersonBoxManagement.PrintAgent.csproj MothersonBoxManagement.PrintAgent/
RUN dotnet restore Motherson_Box_Management.sln

COPY . .
RUN dotnet publish MothersonBoxManagement/MothersonBoxManagement.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.28 AS final
WORKDIR /app

# Temporary root elevation to install curl (required for HEALTHCHECK) and create keys directory.
# The container switches back to the non-root $APP_UID user immediately after.
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /keys \
    && chown -R $APP_UID:$APP_UID /keys

COPY --from=build /app/publish .

EXPOSE 8443

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=4 \
    CMD curl --fail --silent --show-error --insecure https://localhost:8443/health/ready || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "MothersonBoxManagement.dll"]
