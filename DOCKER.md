# Forex Docker Deployment Guide

## 📂 Project Structure

```
forex/
├── docker-compose.yml       # Services configuration (uses pre-built image)
├── .env                     # Environment variables (gitignored)
├── .env.example             # Template for .env
└── src/backend/Forex.WebApi/
    └── Dockerfile           # For building image locally
```

## 🚀 Quick Start

### Option 1: Server Deployment (Pre-built Image)

**This is the recommended way for production servers.**

1. **Copy files to server:**
   ```bash
   # Only these files needed on server:
   scp docker-compose.yml server:/opt/forex/
   scp .env.example server:/opt/forex/
   ```

2. **Configure on server:**
   ```bash
   cd /opt/forex
   cp .env.example .env
   nano .env
   
   # Edit these values:
   # - DOCKER_IMAGE=your-dockerhub-username/forex (or your registry)
   # - MINIO_PUBLIC_ENDPOINT=http://YOUR_SERVER_IP:9000
   # - POSTGRES_PASSWORD=strong_password
   # - JWT_SECRET_KEY=long_random_key
   ```

3. **Load Docker image** (one of these methods):
   
   **Method A: From Docker Hub/Registry**
   ```bash
   # Pull from your registry (uses DOCKER_IMAGE from .env)
   docker-compose pull
   ```
   
   **Method B: From saved tar file**
   ```bash
   # On development machine (where you built):
   docker save muqimjon/forex:latest -o forex-latest.tar
   
   # Copy to server:
   scp forex-latest.tar server:/opt/forex/
   
   # On server:
   docker load -i forex-latest.tar
   ```

4. **Start services:**
   ```bash
   docker-compose up -d
   ```

### Option 2: Local Development (Build from Source)

**For developers who need to build the image locally:**

```bash
# Build the image with your username
docker build -t muqimjon/forex:latest -f src/backend/Forex.WebApi/Dockerfile .

# Then use docker-compose
docker-compose up -d
```

### Option 3: Localhost Testing (Zero Config)

**Just for testing with included .env:**

```bash
# Build image with your tag
docker build -t muqimjon/forex:latest -f src/backend/Forex.WebApi/Dockerfile .

# Start everything
docker-compose up -d
```

| Service | URL | Credentials |
|---------|-----|-------------|
| Backend API | http://localhost:5001 | JWT auth required |
| API Documentation | http://localhost:5001/scalar/v1 | - |
| MinIO Console | http://localhost:9001 | minioadmin / minioadmin |
| PgAdmin | http://localhost:8080 | admin@gmail.com / admin_password |
| PostgreSQL | localhost:5433 | See .env |

## 🛠️ Management Commands

**View logs:**
```bash
docker-compose logs -f [service-name]
# Examples:
docker-compose logs -f app          # Backend logs
docker-compose logs -f postgres     # Database logs
```

**Restart services:**
```bash
docker-compose restart
docker-compose restart app          # Restart only backend
```

**Stop services:**
```bash
docker-compose down
```

**Update backend image** (on server):
```bash
# Method 1: Pull new image from registry
docker pull yourregistry/forex:latest
docker tag yourregistry/forex:latest forex:latest
docker-compose up -d

# Method 2: Load from tar file
docker load -i forex-latest.tar
docker-compose up -d
```

**Clean everything (including data):**
```bash
docker-compose down -v
rm -rf docker-data/
```

## 🔧 Development Workflow

### Building the Image Locally

```bash
# From project root (use your Docker Hub username)
docker build -t muqimjon/forex:latest -f src/backend/Forex.WebApi/Dockerfile .

# Verify
docker images | grep forex
```

### Testing Locally

```bash
# Start all services
docker-compose up -d

# Watch logs
docker-compose logs -f app
```

### Preparing for Server Deployment

**Option A: Push to Docker Hub (Recommended)**
```bash
# Login to Docker Hub
docker login

# Push (image name from .env: muqimjon/forex)
docker push muqimjon/forex:latest

# On server:
docker-compose pull  # Uses DOCKER_IMAGE from .env
```

**Option B: Export as tar file**
```bash
# Save image
docker save muqimjon/forex:latest -o forex-latest.tar
gzip forex-latest.tar  # Optional: compress

# Transfer to server
scp forex-latest.tar.gz server:/opt/forex/

# On server:
gunzip forex-latest.tar.gz
docker load -i forex-latest.tar
```

### Database Access
```bash
# Via psql
docker exec -it forex_postgres psql -U postgres -d forex

# Via PgAdmin
# Open http://localhost:8080 and add server:
# Host: postgres
# Port: 5432
# Username: postgres
# Password: (from .env)
```

### MinIO Access
```bash
# Console: http://localhost:9001
# API: http://localhost:9000
# Bucket: forex-storage
```

## 📋 Configuration Files

### Configuration Files (.env)

The repository includes a ready-to-use `.env` file for localhost testing:

```bash
# Docker Image
DOCKER_IMAGE=muqimjon/forex
DOCKER_TAG=latest

# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=root
POSTGRES_DB=forex

# PgAdmin
PGADMIN_EMAIL=admin@gmail.com
PGADMIN_PASSWORD=admin_password

# MinIO
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin
MINIO_BUCKET_NAME=forex-storage

# Backend
JWT_SECRET_KEY=super_super_secret_key_12345_67890!@#
JWT_ISSUER=Forex
JWT_AUDIENCE=AuditoryForex

# MinIO Public Endpoint
MINIO_PUBLIC_ENDPOINT=http://localhost:9000

# File Upload
MAX_FILE_SIZE_MB=1
```

**These values work out-of-the-box for localhost testing!**

### .env.example (Production Template)

For production deployment, refer to `.env.example` which contains:
- `DOCKER_IMAGE=forex` (change to your registry/username)
- Detailed explanations for each variable
- Security recommendations
- Example placeholder values

**Important:** 
- Change `DOCKER_IMAGE` to your Docker Hub username or registry
- Replace all example values with real credentials in production!

## 🔒 Production Deployment

### Security Checklist
- [ ] Change all default passwords
- [ ] Use strong JWT secret (64+ characters)
- [ ] Set `MINIO_PUBLIC_ENDPOINT` to production domain
- [ ] Configure firewall (allow only 80, 443, 5001)
- [ ] Use HTTPS (add reverse proxy like Nginx)
- [ ] Regular backups of `docker-data/`

### Recommended Production Setup
```bash
# Use HTTPS with reverse proxy
MINIO_PUBLIC_ENDPOINT=https://storage.yourdomain.com

# Strong passwords
POSTGRES_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET_KEY=$(openssl rand -base64 64)
```

## 🐛 Troubleshooting

### Port Already in Use
```bash
# Windows: Check port
netstat -ano | findstr :5001

# Change port in docker-compose.yml
ports:
  - "5002:8080"  # Use 5002 instead
```

### Container Won't Start
```bash
# Check logs
docker-compose logs app

# Restart with fresh build
docker-compose down
docker-compose up -d --build
```

### Database Connection Failed
```bash
# Wait for PostgreSQL to be ready
docker-compose logs postgres

# Should see: "database system is ready to accept connections"
```

### MinIO Access Denied
- Check `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` in .env
- Bucket is created automatically by backend on first request
- Ensure `MINIO_PUBLIC_ENDPOINT` matches your access URL

## 📦 Backup & Restore

### Backup
```bash
# Database
docker exec forex_postgres pg_dump -U postgres forex > backup.sql

# MinIO data
tar -czf minio-backup.tar.gz docker-data/minio/
```

### Restore
```bash
# Database
cat backup.sql | docker exec -i forex_postgres psql -U postgres forex

# MinIO data
tar -xzf minio-backup.tar.gz
```

## 🔗 Related Documentation

- [Main README](README.md) - Project overview
- [Dockerfile](src/backend/Forex.WebApi/Dockerfile) - Backend image
- [.env.example](.env.example) - Configuration template
