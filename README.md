# MetroFlow AI Backend

Backend demo para MetroFlow AI: API ASP.NET Core con arquitectura por capas, MySQL como fuente transaccional, grafo de rutas con Dijkstra, ChromaDB como busqueda semantica con fallback y asistente guiado numerico.

## Requisitos

- .NET 10 SDK
- Docker

## Ejecutar

```bash
docker compose up -d
dotnet run --project Api/Api.csproj
```

URLs:

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health: `http://localhost:5000/health`
- ChromaDB: `http://localhost:8001`
- n8n: `http://localhost:5678`

## Proyectos

- `Domain`: entidades, value objects y enums.
- `Application`: contratos, casos de uso, validaciones, routing y vector search abstractions.
- `Infrastructure`: EF Core, MySQL, seeders, ChromaDB, grafo y servicios concretos.
- `Api`: controladores HTTP, Swagger, CORS y configuracion.

## Endpoints principales

- `POST /api/assistant/start`
- `POST /api/assistant/step`
- `GET /api/public/stations`
- `GET /api/public/routes`
- `GET /api/public/alerts`
- `GET /api/public/stations/{stationId}/arrivals`
- `GET /api/public/routes/{routeId}/occupancy`
- `POST /api/public/route-plan`
- `POST /api/public/semantic-search`
- `GET /api/admin/dashboard`
- `POST /api/admin/vector/reindex`
- `POST /api/admin/graph/rebuild`
