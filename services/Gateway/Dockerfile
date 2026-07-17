FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Gateway.Api/Gateway.Api.csproj Gateway.Api/
RUN dotnet restore Gateway.Api/Gateway.Api.csproj

COPY Gateway.Api/ Gateway.Api/
WORKDIR /src/Gateway.Api
RUN dotnet publish Gateway.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
