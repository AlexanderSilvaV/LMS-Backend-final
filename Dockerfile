FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["LMSBackend.API/LMSBackend.API.csproj", "LMSBackend.API/"]
RUN dotnet restore "LMSBackend.API/LMSBackend.API.csproj"
COPY . .
WORKDIR "/src/LMSBackend.API"
RUN dotnet publish "LMSBackend.API.csproj" -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
ENTRYPOINT ["dotnet", "LMSBackend.API.dll"]
