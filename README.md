# Structured Logging in .NET with Serilog and ClickHouse — Demo

A companion demo for the blog post [Structured Logging in .NET with Serilog and ClickHouse](TODO LINK).

An ASP.NET minimal API that writes structured logs directly to ClickHouse via [Serilog.Sinks.ClickHouse](https://github.com/ClickHouse/Serilog.Sinks.ClickHouse). Logs are queryable with SQL through ClickHouse's built-in Play UI, and the demo also includes [ClickStack](https://clickhouse.com/docs/en/observability/clickstack) for a search-and-filter UI.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (with Docker Compose)

## Quick Start

```bash
# 1. Start everything
docker compose up -d

# 2. Wait ~15 seconds for ClickHouse and the API to start, then generate traffic
curl -X POST http://localhost:5000/generate-traffic

# 3. Query your logs
#    SQL:        http://localhost:8123/play
#    ClickStack: http://localhost:8080
```

## Querying Logs

**SQL (Play UI):** Open [http://localhost:8123/play](http://localhost:8123/play) and run queries directly against `logs.app_logs`.

**ClickStack UI:** Open [http://localhost:8080](http://localhost:8080). A data source and a dashboard have been preconfigured so you can go straight to the data.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check (Debug log) |
| `GET` | `/products` | List all products |
| `GET` | `/products/{id}` | Get product by ID |
| `POST` | `/products` | Create a product |
| `POST` | `/orders` | Place an order |
| `GET` | `/orders/{id}` | Get an order |
| `GET` | `/slow` | Simulate a slow request (50–3000ms) |
| `POST` | `/chaos` | Trigger a random exception |
| `POST` | `/generate-traffic` | Exercise all endpoints to produce diverse logs |

## Project Structure

```
├── docker-compose.yml          # ClickStack + Demo API
├── Dockerfile                  # Multi-stage .NET build
├── clickstack/
│   ├── dashboard.json          # Pre-built HyperDX dashboard
│   └── entry.sh                # ClickStack entrypoint with dashboard import
├── images/                     # Screenshots for the blog post
├── src/DemoApi/
│   ├── Program.cs              # Serilog + sink config, endpoint registration
│   ├── appsettings.json        # Log level overrides
│   ├── Endpoints/
│   │   └── EndpointsUtils.cs   # Endpoint definitions and traffic generator
│   ├── Health/
│   │   └── ClickHouseHealthCheck.cs
│   ├── Middleware/
│   │   └── CorrelationIdMiddleware.cs
│   ├── Models/
│   │   ├── Product.cs
│   │   ├── CreateProductRequest.cs
│   │   ├── OrderRequest.cs
│   │   └── OrderResult.cs
│   └── Services/
│       ├── ProductService.cs   # In-memory product catalog
│       └── OrderService.cs     # Order processing with simulated failures
└── blog/
    └── structured-logging-dotnet-serilog-clickhouse.md
```

## Teardown

```bash
docker compose down -v
```

## License

Apache-2.0
