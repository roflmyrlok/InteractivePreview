# InteractivePreview — repo guide for agents

## Branch layout

| Branch | Purpose | Merges to main? |
|---|---|---|
| `main` | Backend services, admin SPA, infra | — |
| `data-acquisition` | DataAcquisition batch pipeline only | **Never** |

`data-acquisition` is a **permanent parallel branch**. It is never merged into `main` or any other branch. It exists solely so the pipeline code lives in the same repo without polluting the deployable surface. Treat it like a separate repo that happens to share the same git remote.

- Do not open PRs from `data-acquisition` to `main`
- Do not cherry-pick DataAcquisition/ commits onto `main`
- Do not add backend/admin-app/ci-cd files to `data-acquisition`

## What lives where

### main branch
```
backend/
  UserService/          .NET 8 — user auth, JWT issuance
  LocationService/      .NET 8 — shelter locations CRUD
  ReviewService/        .NET 8 — reviews + S3 image uploads
  SourceRegistryService/ .NET 8 — data source registry, AI discovery
admin-app/              React + Vite + TypeScript — admin SPA (served at /admin/)
ci-cd/
  docker-compose.yml    All services + nginx + postgres
  nginx.conf            Reverse proxy routing
.github/workflows/
  deploy.yml            Build Docker images, push to Hub, restart on self-hosted runner
```

### data-acquisition branch
```
DataAcquisition/
  DataAcquisitionSolution/
    Shelter.Shared/     Shared models, parsers, RegistryClient (HTTP client for SourceRegistryService)
    Shelter.Research/   Top-down research runner — fetches sources, AI-maps fields, writes output.json
    Shelter.Ingestion/  Uploads processed output.json to LocationService
    Kyiv.Data/          One-off Kyiv GIS import
  Oblasts/              Source files and output.json per oblast/hromada (gitignored build artifacts excluded)
  Schema/               shelter-schema.json field definitions
```

## Backend service patterns

All four backend services follow the same structure:
- **4 projects**: `Domain` / `Application` / `Infrastructure` / `API`
- **Auth**: JWT Bearer, Issuer=`InteractiveMap.UserService`, Audience=`InteractiveMap`
- **Roles**: `Admin`, `SuperAdmin`, `ServiceAccount` — ServiceAccount cannot access SourceRegistryService
- **DB**: PostgreSQL via EF Core + Npgsql; soft delete with global query filter; `xmin` RowVersion for optimistic concurrency
- **Pattern**: CQRS with MediatR, FluentValidation, Repository pattern

### xmin migration pattern
EF Core generates `AddColumn<uint>` for xmin RowVersion — **manually remove** that line from every CreateTable call in migrations. xmin is a PostgreSQL system column and must not be explicitly added. See any existing migration for reference.

## SourceRegistryService specifics

- Seed data: `ukraine-admin.json` embedded resource — all 25 oblasts + ~1470 hromadas
- Seeder runs on every startup and **upserts** (matches by Code/Slug, updates names, inserts missing rows)
- AI discovery: `POST /api/discovery/hromada/{id}` — calls Anthropic API directly via raw HttpClient (not SDK) with `web_search_20250305` tool; enforces `.gov.ua` URLs
- `--import-from-filesystem <path>` startup arg imports existing DataAcquisition source JSON files as Manual/Active sources

## Admin SPA hosting

Built as static files during CI (`npm ci && npm run build`), copied into a Docker named volume `admin-dist`, served by nginx at `/admin/`. No Node.js process at runtime.

## CI/CD

- Self-hosted runner on `main` pushes only
- Builds and pushes Docker images to Docker Hub (tagged with sha + latest)
- Copies admin SPA dist into the `admin-dist` volume via a temp Alpine container
- Restarts all services with `docker-compose up -d --force-recreate`
- `.env` is copied from the server's local filesystem (not in repo)
