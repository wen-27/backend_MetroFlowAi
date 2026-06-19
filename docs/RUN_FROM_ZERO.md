# MetroFlow AI - correr el proyecto desde cero

Esta guia sirve para probar el backend, Postgres, ChromaDB y el frontend en otro computador.

## 1. Requisitos

- .NET SDK compatible con el proyecto.
- Node.js y npm.
- PostgreSQL instalado y corriendo.
- DBeaver opcional para administrar la base.
- ChromaDB instalado con Python.

Si el comando `chroma` no existe, en este computador se usa:

```bash
/Users/wen/Library/Python/3.9/bin/chroma
```

## 2. Crear la base en PostgreSQL

Abre DBeaver o cualquier cliente PostgreSQL y crea la base:

```sql
CREATE DATABASE metroflow_ai;
```

Si la base `metroflow_ai` ya existe, no la crees otra vez.

La conexion configurada actualmente en el backend es:

```text
Host=localhost
Port=5433
Database=metroflow_ai
Username=postgres
Password=27072008_wen
```

El archivo de configuracion esta en:

```text
Api/appsettings.json
```

## 3. Aplicar migraciones del backend

En una terminal:

```bash
cd /Users/wen/Desktop/Reto
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --context MetroFlowDbContext
```

Esto crea las tablas en PostgreSQL.

## 4. Correr el backend

En la misma terminal, o en otra:

```bash
cd /Users/wen/Desktop/Reto
dotnet run --project Api/Api.csproj --urls http://localhost:5000
```

El backend queda en:

```text
http://localhost:5000
```

Swagger queda en:

```text
http://localhost:5000/swagger
```

Para abrir Swagger desde terminal en macOS:

```bash
open http://localhost:5000/swagger
```

## 5. Correr ChromaDB

En otra terminal:

```bash
cd /Users/wen/Desktop/Reto
/Users/wen/Library/Python/3.9/bin/chroma run --path ./chroma-data --host localhost --port 8001
```

ChromaDB queda en:

```text
http://localhost:8001
```

Para comprobar que ChromaDB responde:

```bash
curl http://localhost:8001/api/v2/heartbeat
```

## 6. Cargar datos base en PostgreSQL

Con el backend corriendo en `http://localhost:5000`, en otra terminal ejecuta:

```bash
curl -X POST http://localhost:5000/api/app/reset
```

Esto carga los seeders base:

- Rutas `R1` a `R6`.
- Estaciones `ST-01` a `ST-10`.
- Buses.
- Alertas.
- Incidentes.
- Recomendaciones.
- Coordenadas de rutas para el mapa.

## 7. Reindexar la base vectorial

Con backend y ChromaDB corriendo:

```bash
curl -X POST http://localhost:5000/api/admin/vector/reindex
```

Debe responder algo parecido a:

```json
{"indexedDocuments":21}
```

## 8. Correr el frontend

En otra terminal:

```bash
cd /Users/wen/Desktop/Frontend_MetroFlowIA
npm install
npm run dev
```

El frontend queda en:

```text
http://localhost:3000
```

Para abrirlo desde terminal en macOS:

```bash
open http://localhost:3000
```

## 9. Verificar que todo funciona

Backend:

```bash
curl http://localhost:5000/health
```

Estado que consume el frontend:

```bash
curl http://localhost:5000/api/app/state
```

Debe devolver datos como:

- `routes`: 6
- `stations`: 10
- `buses`: 8
- `alerts`: 3
- `incidents`: 2
- `recommendations`: 3
- `peakDemandForecast`: 15

Busqueda semantica con ChromaDB:

```bash
curl -X POST http://localhost:5000/api/public/semantic-search \
  -H 'Content-Type: application/json' \
  -d '{"query":"Portal Provenza ocupacion alta","limit":3}'
```

## 10. Si un puerto ya esta ocupado

Si al correr el backend aparece:

```text
address already in use
```

significa que ya hay una copia corriendo en ese puerto.

Para ver quien usa el puerto `5000`:

```bash
lsof -nP -iTCP:5000 -sTCP:LISTEN
```

Para apagar ese proceso, usa el PID que salga. Ejemplo:

```bash
kill 67294
```

Si no se apaga:

```bash
kill -9 67294
```

Luego vuelve a correr:

```bash
dotnet run --project Api/Api.csproj --urls http://localhost:5000
```

## 11. Orden recomendado de terminales

Terminal 1 - backend:

```bash
cd /Users/wen/Desktop/Reto
dotnet run --project Api/Api.csproj --urls http://localhost:5000
```

Terminal 2 - ChromaDB:

```bash
cd /Users/wen/Desktop/Reto
/Users/wen/Library/Python/3.9/bin/chroma run --path ./chroma-data --host localhost --port 8001
```

Terminal 3 - comandos de preparacion:

```bash
curl -X POST http://localhost:5000/api/app/reset
curl -X POST http://localhost:5000/api/admin/vector/reindex
```

Terminal 4 - frontend:

```bash
cd /Users/wen/Desktop/Frontend_MetroFlowIA
npm install
npm run dev
```

Despues abre:

```text
http://localhost:3000
http://localhost:5000/swagger
```
