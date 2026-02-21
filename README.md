# Forex
Business Management System

<img width="930" height="942" alt="image" src="https://github.com/user-attachments/assets/b180adf4-a103-4d67-a99c-6618477e7d08" />

## 🏗️ Architecture

**Clean Architecture** with following layers:
- **Domain**: Business entities and core logic
- **Application**: Use cases, DTOs, and interfaces
- **Infrastructure**: Data access, file storage (MinIO), external services
- **WebApi**: REST API endpoints
- **WPF**: Desktop client application

## 🚀 Quick Start

### Test Locally (Zero Configuration!)

The repository includes working `.env` file - just run:

```bash
# 1. Build the image with your Docker Hub username
docker build -t muqimjon/forex:latest -f src/backend/Forex.WebApi/Dockerfile .

# 2. Start all services
docker-compose up -d

# Access immediately:
# API Docs: http://localhost:5001/scalar/v1
# MinIO Console: http://localhost:9001 (minioadmin/minioadmin)
# PgAdmin: http://localhost:8080 (admin@gmail.com/admin_password)
```

### Production Deployment (Pre-built Image)

For production server, you don't need the source code - just the image:

```bash
# On server, prepare environment:
nano .env  # Edit values:
# DOCKER_IMAGE=muqimjon/forex (or your-username/forex)
# MINIO_PUBLIC_ENDPOINT=http://YOUR_SERVER_IP:9000
# POSTGRES_PASSWORD=strong_password
# JWT_SECRET_KEY=long_random_key

# Pull image from Docker Hub (if pushed):
docker-compose pull

# Or load from tar file:
docker load -i forex-latest.tar

# Start services
docker-compose up -d
```

📖 See [DOCKER.md](DOCKER.md) for detailed deployment guide.

### Development Setup (Visual Studio)

1. **Start MinIO** (standalone):
   ```bash
   cd C:\Users\muqim\OneDrive\Ishchi stol\dockerize\minIO
   docker-compose up -d
   ```

2. **Run Backend** in Visual Studio:
   - Set `Forex.WebApi` as startup project
   - Uses `appsettings.Development.json`

3. **Run WPF Client**:
   - Set `Forex.Wpf` as startup project
   - Configure backend URL in `appsettings.json`

## 📦 Features

### File Storage (MinIO)
- Automatic image compression (max 500KB)
- Telegram-style optimization (1920px max dimension)
- Dual-client architecture for Docker networking
- Presigned URL generation for secure uploads

### Image Upload
- **Frontend**: Automatic compression before upload
- **Backend**: 1MB max file size validation
- **Preview**: Instant preview when image selected
- **Quality**: JPEG quality 82% (70% fallback if needed)

## 🔧 Configuration

### Backend (appsettings.json)
```json
{
  "Minio": {
    "Endpoint": "minio:9000",
    "PublicEndpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "forex-storage"
  },
  "FileUpload": {
    "MaxFileSizeMB": 1
  }
}
```

### Environment Variables (.env)
```bash
# Change for production
MINIO_PUBLIC_ENDPOINT=http://your-server.com:9000
POSTGRES_PASSWORD=strong_password_here
JWT_SECRET_KEY=long_random_secret_key
```

See [.env.example](.env.example) for all available options.

## 🌐 API Access

- **Swagger/Scalar UI**: http://localhost:5001/scalar/v1
- **API Endpoint**: http://localhost:5001

## 📁 Project Structure

```
forex/
├── docker-compose.yml          # Docker services
├── .env                        # Environment (gitignored)
├── .env.example                # Config template
├── DOCKER.md                   # Deployment guide
├── src/
│   ├── backend/
│   │   ├── Forex.Domain/
│   │   ├── Forex.Application/
│   │   ├── Forex.Infrastructure/
│   │   └── Forex.WebApi/
│   │       └── Dockerfile      # Backend image
│   └── frontend/
│       ├── Forex.ClientService/
│       └── Forex.Wpf/
└── docker-data/                # Volumes (gitignored)
    ├── pgdata/
    └── minio/
```

## 📝 Technologies

- **.NET 9.0**
- **PostgreSQL** - Database
- **MinIO** - Object Storage
- **WPF** - Desktop Client
- **Docker** - Containerization
- **JWT** - Authentication
- **ImageSharp** - Image Processing

## 📚 Documentation

- [DOCKER.md](DOCKER.md) - Deployment & troubleshooting
- [.env.example](.env.example) - Configuration reference
- API documentation available at `/scalar/v1` when running
