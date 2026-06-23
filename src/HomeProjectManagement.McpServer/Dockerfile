# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution-wide build config first, for better layer caching. (Central Package
# Management + shared MSBuild props are required by every project's restore.)
COPY Directory.Build.props Directory.Packages.props ./

# Restore against just the csproj graph the MCP server needs.
COPY src/HomeProjectManagement.McpServer/*.csproj       src/HomeProjectManagement.McpServer/
COPY src/HomeProjectManagement.Application/*.csproj      src/HomeProjectManagement.Application/
COPY src/HomeProjectManagement.Domain/*.csproj          src/HomeProjectManagement.Domain/
COPY src/HomeProjectManagement.Infrastructure/*.csproj  src/HomeProjectManagement.Infrastructure/
COPY src/HomeProjectManagement.ServiceDefaults/*.csproj src/HomeProjectManagement.ServiceDefaults/
RUN dotnet restore src/HomeProjectManagement.McpServer/HomeProjectManagement.McpServer.csproj

# Copy sources and publish.
COPY src/ src/
RUN dotnet publish src/HomeProjectManagement.McpServer/HomeProjectManagement.McpServer.csproj \
    -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Run as the non-root user provided by the base image.
USER $APP_UID
ENTRYPOINT ["dotnet", "HomeProjectManagement.McpServer.dll"]
