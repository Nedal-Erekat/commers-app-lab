FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OrderProcessing.Worker/OrderProcessing.Worker.csproj OrderProcessing.Worker/
RUN dotnet restore OrderProcessing.Worker/OrderProcessing.Worker.csproj

COPY OrderProcessing.Worker/ OrderProcessing.Worker/
WORKDIR /src/OrderProcessing.Worker
RUN dotnet publish OrderProcessing.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderProcessing.Worker.dll"]
