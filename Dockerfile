# Pobranie obrazu .NET SDK do budowania aplikacji
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Kopiowanie plików projektu i przywracanie zależności
COPY . . 
RUN dotnet restore

# Budowanie aplikacji
RUN dotnet publish -c Release -o /out

# Pobranie obrazu runtime .NET
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Uruchamianie aplikacji
ENTRYPOINT ["dotnet", "DocumentProcessorApi.dll"]
