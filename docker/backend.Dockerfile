FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ .

RUN dotnet publish Hysteria2Dashboard.API/Hysteria2Dashboard.API.csproj \
    -c Release -r linux-x64 --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Hysteria2Dashboard.API.dll"]