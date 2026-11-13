FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
# Visual Studio uses port 8081 for its HTTPS launch profile.
EXPOSE 8080 8081
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Mars_Survival_Hanbook.csproj ./
RUN dotnet restore Mars_Survival_Hanbook.csproj

COPY . ./
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Mars_Survival_Hanbook.csproj \
    -c "$BUILD_CONFIGURATION" --no-restore -p:UseAppHost=false -o /out

FROM base AS final
COPY --from=build /out/ ./
USER root
RUN mkdir -p App_Data/raw ImageMarHanbook \
    && chown "$APP_UID" App_Data/raw ImageMarHanbook
USER $APP_UID
ENTRYPOINT ["dotnet", "Mars_Survival_Hanbook.dll"]
