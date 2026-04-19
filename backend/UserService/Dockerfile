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

# Copy the entire solution
COPY . .

# Restore dependencies using solution file with --force to ignore conflicts
RUN dotnet restore UserService.sln --force

# Build the API project directly instead of the solution
RUN dotnet build UserService.API/UserService.API.csproj -c Release --no-restore -o /app/build

# ── Test stage ────────────────────────────────────────────────────
# Runs unit tests during `docker build --target test`.
# Source is already COPY'd into /src by the build stage so the
# filesystem is fully writable — no volume mounts, no permission errors.
FROM build AS test
WORKDIR /src
RUN dotnet test UserService.sln -c Release \
    --logger "console;verbosity=normal" \
    --results-directory /app/testresults \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput=/app/testresults/coverage.xml

# ── Publish stage ─────────────────────────────────────────────────
FROM build AS publish
WORKDIR "/src"
RUN dotnet publish UserService.API/UserService.API.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ── Final / runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=8080

# Install postgresql-client for database connection checks
RUN apt-get update && \
    apt-get install -y postgresql-client curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /src .
COPY --from=publish /app/publish .

# Install dotnet-ef tool globally
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

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
echo "Starting UserService..."\n\
dotnet UserService.API.dll\n' > /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]
