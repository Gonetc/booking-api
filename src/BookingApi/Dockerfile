# Build context is the repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/BookingApi/BookingApi.csproj src/BookingApi/
RUN dotnet restore src/BookingApi/BookingApi.csproj

COPY src/BookingApi/ src/BookingApi/
RUN dotnet publish src/BookingApi/BookingApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookingApi.dll"]
