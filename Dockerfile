# Multi-stage build for SeniorSharp: React SPA + ASP.NET Core API (net9.0), served from one container.

# Stage 1: build the React SPA (frontend/) into a static bundle.
FROM node:20-alpine AS frontend
WORKDIR /fe
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
# Build into ./dist here (overrides the dev outDir that points at the API's wwwroot).
RUN npx vite build --outDir dist --emptyOutDir

# Stage 2: restore + publish the .NET API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore.
COPY SeniorSharp.sln ./
COPY Directory.Build.props ./
COPY engine/SeniorSharp.Domain/SeniorSharp.Domain.csproj engine/SeniorSharp.Domain/
COPY engine/SeniorSharp.Contracts/SeniorSharp.Contracts.csproj engine/SeniorSharp.Contracts/
COPY engine/SeniorSharp.Persistence/SeniorSharp.Persistence.csproj engine/SeniorSharp.Persistence/
COPY engine/SeniorSharp.Llm/SeniorSharp.Llm.csproj engine/SeniorSharp.Llm/
COPY engine/SeniorSharp.Orchestration/SeniorSharp.Orchestration.csproj engine/SeniorSharp.Orchestration/
COPY engine/SeniorSharp.Api/SeniorSharp.Api.csproj engine/SeniorSharp.Api/
RUN dotnet restore engine/SeniorSharp.Api/SeniorSharp.Api.csproj

# Copy the rest and publish (wwwroot is .dockerignored, so the SPA comes from the frontend stage).
COPY . .
RUN dotnet publish engine/SeniorSharp.Api/SeniorSharp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 3: lean ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
USER $APP_UID

# Kestrel listens on 8080 (overridable via ASPNETCORE_URLS).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Closed-content (skill graph / criteria / prompts) read at runtime; the published app; the built SPA.
COPY --from=build /src/content ./content
COPY --from=build /app/publish ./
COPY --from=frontend /fe/dist ./wwwroot

ENTRYPOINT ["dotnet", "SeniorSharp.Api.dll"]
