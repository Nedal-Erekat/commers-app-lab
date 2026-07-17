FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so Docker cache is only invalidated on dependency changes
COPY Cart.Domain/Cart.Domain.csproj Cart.Domain/
COPY Cart.Application/Cart.Application.csproj Cart.Application/
COPY Cart.Infrastructure/Cart.Infrastructure.csproj Cart.Infrastructure/
COPY Cart.Api/Cart.Api.csproj Cart.Api/
RUN dotnet restore Cart.Api/Cart.Api.csproj

# Copy source and publish
COPY Cart.Domain/ Cart.Domain/
COPY Cart.Application/ Cart.Application/
COPY Cart.Infrastructure/ Cart.Infrastructure/
COPY Cart.Api/ Cart.Api/
WORKDIR /src/Cart.Api
RUN dotnet publish Cart.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Cart.Api.dll"]
