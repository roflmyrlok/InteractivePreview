# InteractiveMap

A microservices application built with .NET 8, Entity Framework Core, and PostgreSQL. Three core services (User, Location, Review) provide core functionality for location-based reviews and interactions.

## Table of Contents

- [Architecture](#architecture-overview)
- [Services](#services)
- [Project Structure](#project-structure)
- [Deployment](#deployment)
- [API Documentation](#api-documentation)

## Architecture Overview

The application uses a microservices architecture with the following components:

- **User Service**: Handles user registration, authentication, and management
- **Location Service**: Manages location data and location-based queries
- **Review Service**: Provides functionality for users to review locations
- **PostgreSQL**: Persistent storage for all services

Each service follows a clean architecture pattern with the following layers:
- Domain: Core business logic and entities
- Application: Use cases and business rules
- Infrastructure: Data access, external services
- API: REST endpoints and controllers

## Services

### User Service
- User registration and authentication
- User profile management
- JWT token generation for secure API access
- Role-based access control

### Location Service
- Location CRUD operations
- Geospatial queries (nearby locations)
- Location details and properties management
- Location validation for other services

### Review Service
- Review CRUD operations
- Location rating system
- User-specific reviews
- Aggregate review metrics for locations

## Project Structure

```
InteractiveMap/
├── backend/
│   ├── UserService/
│   │   ├── UserService.API
│   │   ├── UserService.Application
│   │   ├── UserService.Domain
│   │   ├── UserService.Infrastructure
│   │   └── UserService.Tests
│   ├── LocationService/
│   │   ├── LocationService.API
│   │   ├── LocationService.Application
│   │   ├── LocationService.Domain
│   │   ├── LocationService.Infrastructure
│   │   └── LocationService.Tests
│   └── ReviewService/
│       ├── ReviewService.API
│       ├── ReviewService.Application
│       ├── ReviewService.Domain
│       ├── ReviewService.Infrastructure
│       └── ReviewService.Tests
├── ci-cd/
│   ├── docker-compose.yml      (Production deployment)
│   ├── .env.example             (Configuration template)
│   ├── nginx.conf               (Reverse proxy)
│   ├── init-db.sh               (Database initialization)
│   └── README.md                (Deployment guide)
└── README.md
```

## Deployment

This project is deployed to a **single EC2 instance** using Docker Compose.

### Quick Start on EC2

1. **Configure environment:**
   ```bash
   cd ci-cd
   cp .env.example .env
   nano .env  # Update with your values
   ```

2. **Start the stack:**
   ```bash
   docker compose up -d
   ```

3. **Verify deployment:**
   ```bash
   docker compose ps
   docker compose logs -f
   ```

### Environment Variables

Required values in `.env`:
- `POSTGRES_PASSWORD` - Database password
- `JWT_KEY` - Generate with: `openssl rand -base64 32`
- `DOMAIN_NAME` - Your EC2 domain or public IP
- `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` - For S3 uploads
- `S3_BUCKET_NAME`, `S3_REGION`, `S3_BASE_URL` - S3 configuration

### Common Commands

```bash
docker compose ps                      # Check service status
docker compose logs -f                 # View logs
docker compose restart <service>       # Restart a service
docker compose down                    # Stop all services
docker compose up -d --build           # Rebuild and restart
```

For complete deployment instructions, see [ci-cd/README.md](ci-cd/README.md).


## API Documentation

See [API-Documentation.md](API-Documentation.md) for complete API specifications.

## Architecture Notes

### Services
- **UserService** - User registration, authentication, JWT token generation
- **LocationService** - Location CRUD, geospatial queries
- **ReviewService** - Review CRUD, ratings, S3 file uploads

### Database
- Single PostgreSQL instance shared across all services
- Automatic initialization via `init-db.sh` on first startup

### Infrastructure
- **Nginx** - Reverse proxy (ports 80/443)
- **Docker** - Containerization
- **Docker Compose** - Orchestration

### Production Environment
- All services run in **Production mode** (Swagger disabled for security)
- Nginx handles routing and HTTPS termination
- Services communicate over internal network
- Single EC2 instance deployment
