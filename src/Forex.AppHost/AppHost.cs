var builder = DistributedApplication.CreateBuilder(args);

// ==============================================================================
// 1. Databases & Infrastructure
// ==============================================================================

var postgres = builder.AddPostgres("postgres-server")
    .WithDataVolume()
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(8080));

var db = postgres.AddDatabase("forex");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithBindMount("minio-data", "/data");

// ==============================================================================
// 2. API Services
// ==============================================================================

var apiService = builder.AddProject<Projects.Forex_WebApi>("forex-webapi")
    .WaitFor(db)
    .WithReference(db)
    .WithEnvironment("Minio__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("Minio__AccessKey", "minioadmin")
    .WithEnvironment("Minio__SecretKey", "minioadmin")
    .WithEnvironment("Minio__BucketName", "products")
    .WithEnvironment("Minio__UseSsl", "false")
    .WithEnvironment("Minio__EnablePublicRead", "true");

// ==============================================================================
// 3. Application Startup
// ==============================================================================

builder.Build().Run();