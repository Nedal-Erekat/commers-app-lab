FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so Docker cache is only invalidated on dependency changes
COPY Order.Domain/Order.Domain.csproj Order.Domain/
COPY Order.Application/Order.Application.csproj Order.Application/
COPY Order.Infrastructure/Order.Infrastructure.csproj Order.Infrastructure/
COPY Order.Api/Order.Api.csproj Order.Api/
RUN dotnet restore Order.Api/Order.Api.csproj

# Copy source and publish
COPY Order.Domain/ Order.Domain/
COPY Order.Application/ Order.Application/
COPY Order.Infrastructure/ Order.Infrastructure/
COPY Order.Api/ Order.Api/
WORKDIR /src/Order.Api
RUN dotnet publish Order.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Api.dll"]
