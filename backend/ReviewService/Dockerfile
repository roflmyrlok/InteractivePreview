# ─────────────────────────────────────────────────────────────────
# No --platform flag here.  Platform is controlled at the compose
# level so the same Dockerfile works on:
#   • Apple Silicon / ARM64 (local dev & CI)  — runs native
#   • EC2 x86-64                              — docker-compose.yml
#                                               sets platform: linux/amd64
# ─────────────────────────────────────────────────────────────────

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore ReviewService.sln

RUN dotnet build ReviewService.API/ReviewService.API.csproj -c Release -o /app/build

# ── Test stage ────────────────────────────────────────────────────
FROM build AS test
WORKDIR /src
RUN dotnet test ReviewService.sln -c Release \
    --logger "console;verbosity=normal" \
    --results-directory /app/testresults \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput=/app/testresults/coverage.xml

# ── Publish stage ─────────────────────────────────────────────────
FROM build AS publish
WORKDIR "/src"
RUN dotnet publish ReviewService.API/ReviewService.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── Final / runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=8080

RUN apt-get update && \
    apt-get install -y postgresql-client curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /src .
COPY --from=publish /app/publish .

RUN printf '#!/bin/bash\n\
DB_HOST=$(echo $ConnectionStrings__DefaultConnection | grep -o "Host=[^;]*" | cut -d= -f2)\n\
DB_PORT=$(echo $ConnectionStrings__DefaultConnection | grep -o "Port=[^;]*" | cut -d= -f2)\n\
DB_USER=$(echo $ConnectionStrings__DefaultConnection | grep -o "Username=[^;]*" | cut -d= -f2)\n\
\n\
echo "Waiting for database at $DB_HOST:$DB_PORT..."\n\
until pg_isready -h $DB_HOST -p $DB_PORT -U $DB_USER; do\n\
  echo "Waiting for database..."\n\
  sleep 5\n\
done\n\
\n\
echo "Starting ReviewService..."\n\
dotnet ReviewService.API.dll\n' > /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]
