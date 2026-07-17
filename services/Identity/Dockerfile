FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so Docker cache is only invalidated on dependency changes
COPY Identity.Domain/Identity.Domain.csproj Identity.Domain/
COPY Identity.Application/Identity.Application.csproj Identity.Application/
COPY Identity.Infrastructure/Identity.Infrastructure.csproj Identity.Infrastructure/
COPY Identity.Api/Identity.Api.csproj Identity.Api/
RUN dotnet restore Identity.Api/Identity.Api.csproj

# Copy source and publish
COPY Identity.Domain/ Identity.Domain/
COPY Identity.Application/ Identity.Application/
COPY Identity.Infrastructure/ Identity.Infrastructure/
COPY Identity.Api/ Identity.Api/
WORKDIR /src/Identity.Api
RUN dotnet publish Identity.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
