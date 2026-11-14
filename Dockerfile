FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /codigo

COPY global.json Directory.Build.props .editorconfig CodeMetricsConfig.txt ./
COPY src/Sophia.Domain/Sophia.Domain.csproj src/Sophia.Domain/
COPY src/Sophia.Application/Sophia.Application.csproj src/Sophia.Application/
COPY src/Sophia.Infrastructure/Sophia.Infrastructure.csproj src/Sophia.Infrastructure/
COPY src/Sophia.Web/Sophia.Web.csproj src/Sophia.Web/
RUN dotnet restore src/Sophia.Web/Sophia.Web.csproj

COPY src/ src/
RUN dotnet publish src/Sophia.Web/Sophia.Web.csproj -c Release -o /publicacao --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    TZ=America/Sao_Paulo
COPY --from=build /publicacao .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sophia.Web.dll"]
