FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Motherson_Box_Management.sln ./
COPY MothersonBoxManagement/MothersonBoxManagement.csproj MothersonBoxManagement/
COPY MothersonBoxManagement.Tests/MothersonBoxManagement.Tests.csproj MothersonBoxManagement.Tests/
RUN dotnet restore Motherson_Box_Management.sln

COPY . .
RUN dotnet publish MothersonBoxManagement/MothersonBoxManagement.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MothersonBoxManagement.dll"]
