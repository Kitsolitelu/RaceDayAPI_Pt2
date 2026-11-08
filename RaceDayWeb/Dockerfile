# ---------- stage 1: build the app ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy only the project file first, so the restored packages are cached between builds
COPY RaceDayWeb.csproj ./
RUN dotnet restore

# now copy the rest of the code and publish a release build
COPY . ./
RUN dotnet publish RaceDayWeb.csproj -c Release -o /app/publish --no-restore

# ---------- stage 2: the small image that actually runs ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# the website listens on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# do not run as root inside the container
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDayWeb.dll"]
