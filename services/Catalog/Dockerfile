FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so Docker cache is only invalidated on dependency changes
COPY Catalog.Domain/Catalog.Domain.csproj Catalog.Domain/
COPY Catalog.Application/Catalog.Application.csproj Catalog.Application/
COPY Catalog.Infrastructure/Catalog.Infrastructure.csproj Catalog.Infrastructure/
COPY Catalog.Api/Catalog.Api.csproj Catalog.Api/
RUN dotnet restore Catalog.Api/Catalog.Api.csproj

# Copy source and publish
COPY Catalog.Domain/ Catalog.Domain/
COPY Catalog.Application/ Catalog.Application/
COPY Catalog.Infrastructure/ Catalog.Infrastructure/
COPY Catalog.Api/ Catalog.Api/
WORKDIR /src/Catalog.Api
RUN dotnet publish Catalog.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
