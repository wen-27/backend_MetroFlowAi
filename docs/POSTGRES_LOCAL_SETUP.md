# PostgreSQL local setup

Este proyecto esta configurado para usar PostgreSQL local sin Docker.

## Credenciales esperadas por defecto

`Api/appsettings.json` usa:

```text
Host=localhost
Port=5433
Database=metroflow_ai
Username=metroflow
Password=metroflow123
```

## Crear base y usuario

Entra a PostgreSQL con tu usuario administrador:

```bash
psql -U postgres
```

Si tu instalacion usa otro usuario admin, cambia `postgres` por ese usuario.

Luego ejecuta:

```sql
CREATE USER metroflow WITH PASSWORD 'metroflow123';
CREATE DATABASE metroflow_ai OWNER metroflow;
GRANT ALL PRIVILEGES ON DATABASE metroflow_ai TO metroflow;
```

Si ya existen, puedes usar:

```sql
ALTER USER metroflow WITH PASSWORD 'metroflow123';
```

Prueba la conexion:

```bash
psql "Host=localhost Port=5433 Database=metroflow_ai Username=metroflow Password=metroflow123"
```

## Aplicar migraciones

Desde la raiz del proyecto:

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext
```

## Usar tus propias credenciales sin subirlas

Crea este archivo local:

```text
Api/appsettings.Local.json
```

No se sube al repo porque esta en `.gitignore`.

Contenido ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=metroflow_ai;Username=postgres;Password=TU_PASSWORD"
  }
}
```

Tambien puedes usar variable de entorno:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=metroflow_ai;Username=postgres;Password=TU_PASSWORD"
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext
dotnet run --project Api/Api.csproj
```
