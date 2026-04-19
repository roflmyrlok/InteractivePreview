# EC2 Deployment

Deploy InteractiveMap to a single EC2 instance using Docker Compose.

## Setup

### 1. Configure environment
```bash
cp .env.example .env
nano .env
```

**Required values to update:**
- `POSTGRES_PASSWORD` - Strong database password
- `JWT_KEY` - Generate with: `openssl rand -base64 32`
- `DOMAIN_NAME` - Your EC2 domain or IP
- AWS credentials for S3 uploads

### 2. Start services
```bash
docker compose up -d
```

### 3. Verify deployment
```bash
docker compose ps                     # Check status
docker compose logs -f                # View logs

# Test services (use EC2 public IP/domain)
curl http://YOUR_EC2_IP/              # Nginx should respond
```

**Note:** Swagger is disabled in Production mode for security. Enable it temporarily by changing `ASPNETCORE_ENVIRONMENT=Development` in docker-compose.yml if needed for debugging.

## Operations

```bash
docker compose restart <service>      # Restart a service
docker compose logs -f <service>      # View service logs
docker compose stop                   # Stop all services
docker compose down                   # Stop and remove containers
docker compose up -d --build          # Rebuild and restart
```

## Files

- `docker-compose.yml` - Stack definition
- `.env.example` - Environment variables template
- `nginx.conf` - Reverse proxy configuration
- `init-db.sh` - Database initialization

## Architecture

```
Nginx (reverse proxy on port 80/443)
├── UserService (internal port 5280)
├── LocationService (internal port 5282)
└── ReviewService (internal port 5284)
      ↓
PostgreSQL database
```
