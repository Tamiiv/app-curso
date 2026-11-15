FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props AppCurso.slnx ./
COPY src/AppCurso.Web/AppCurso.Web.csproj src/AppCurso.Web/
RUN dotnet restore src/AppCurso.Web/AppCurso.Web.csproj

COPY content ./content
COPY src/AppCurso.Web ./src/AppCurso.Web
RUN dotnet publish src/AppCurso.Web/AppCurso.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "AppCurso.Web.dll"]
