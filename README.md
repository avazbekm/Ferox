# Forex
Reporter for middle bussines

<img width="930" height="942" alt="image" src="https://github.com/user-attachments/assets/b180adf4-a103-4d67-a99c-6618477e7d08" />

## Architecture

**Clean Architecture** with following layers:
- **Domain**: Business entities and core logic
- **Application**: Use cases, DTOs, and interfaces
- **Infrastructure**: Data access, file storage, external services
- **WebApi**: REST API endpoints

## File Storage

MinIO object storage integration with dual-client architecture:
- **Internal Client**: Used for bucket operations via Docker network
- **Public Client**: Generates presigned URLs accessible by external clients

### Configuration

```json
{
  "Minio": {
    "Endpoint": "minio:9000",
    "PublicEndpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "BucketName": "forex-storage",
    "Prefix": "uploads"
  }
}
```

### Deployment

For production deployment, override `PublicEndpoint` via environment variable:
```bash
docker-compose up -d
# or
export Minio__PublicEndpoint=http://your-server-ip:9000
```
