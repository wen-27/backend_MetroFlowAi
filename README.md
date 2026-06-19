# MetroFlow AI Backend

Backend demo para MetroFlow AI: API ASP.NET Core con arquitectura por capas, PostgreSQL como fuente transaccional, grafo de rutas con Dijkstra y ChromaDB como busqueda semantica con fallback. El chatbox guiado numerico se controla desde el frontend.

## Requisitos

- .NET 10 SDK
- Docker

## Ejecutar

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext
dotnet run --project Api/Api.csproj
```

URLs:

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health: `http://localhost:5000/health`
- PostgreSQL: `localhost:5433`
- ChromaDB: `http://localhost:8001`
- n8n: `http://localhost:5678`

## ChromaDB sin Docker

```bash
python3 -m pip install chromadb
chroma run --path ./chroma-data --host localhost --port 8001
```

Luego, con el backend corriendo:

```bash
curl -X POST http://localhost:5000/api/admin/vector/reindex
```

## Proyectos

- `Domain`: entidades, value objects y enums.
- `Application`: contratos, casos de uso, validaciones, routing y vector search abstractions.
- `Infrastructure`: EF Core, PostgreSQL, seeders, ChromaDB, grafo y servicios concretos.
- `Api`: controladores HTTP, Swagger, CORS y configuracion.

## Endpoints principales

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

## Migraciones

La migracion inicial ya esta en `Infrastructure/Persistence/Migrations`.

Configuracion local de PostgreSQL: [docs/POSTGRES_LOCAL_SETUP.md](docs/POSTGRES_LOCAL_SETUP.md).

Para crear una nueva migracion:

```bash
dotnet ef migrations add NombreDeLaMigracion --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext --output-dir Persistence/Migrations
```

Para aplicar migraciones:

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext
```

El backend tambien ejecuta `Database.MigrateAsync()` al iniciar, asi que si PostgreSQL esta disponible aplica migraciones pendientes y luego corre seeders idempotentes.

## Chatbox guiado

El backend no guarda sesiones del chatbox ni expone endpoints conversacionales. El frontend maneja el estado numerico y llama estos endpoints segun la opcion seleccionada:

- `GET /api/public/stations`
- `GET /api/public/routes`
- `GET /api/public/alerts`
- `GET /api/public/stations/{stationId}/arrivals`
- `GET /api/public/routes/{routeId}/occupancy`
- `POST /api/public/route-plan`
- `POST /api/public/semantic-search`
