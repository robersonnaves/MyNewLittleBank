FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/MyNewLittleBank.AppHost/MyNewLittleBank.AppHost.csproj"
RUN dotnet publish "src/MyNewLittleBank.AppHost/MyNewLittleBank.AppHost.csproj" -c Release -o /app/publish --no-restore -p:IsPublishable=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "MyNewLittleBank.AppHost.dll"]
