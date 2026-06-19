# ChromaDB

La integracion esta detras de:

- `Application/VectorSearch/Abstractions/IVectorSearchService.cs`
- `Application/VectorSearch/Abstractions/IVectorIndexingService.cs`
- `Infrastructure/VectorSearch/Chroma/ChromaVectorSearchService.cs`
- `Infrastructure/VectorSearch/Chroma/ChromaVectorIndexingService.cs`

`POST /api/admin/vector/reindex` genera documentos de estaciones, rutas, incidentes y recomendaciones.

## Configuracion local sin Docker

ChromaDB corre como un servidor HTTP aparte del backend. Para este proyecto usa:

```text
BaseUrl: http://localhost:8001
CollectionName: metroflow_knowledge
```

Instala ChromaDB con Python:

```bash
python3 -m pip install chromadb
```

Levanta el servidor local persistiendo datos en `./chroma-data`:

```bash
chroma run --path ./chroma-data --host localhost --port 8001
```

Deja ese proceso abierto mientras uses el backend.

La configuracion del backend esta en `Api/appsettings.json`:

```json
{
  "Chroma": {
    "BaseUrl": "http://localhost:8001",
    "CollectionName": "metroflow_knowledge"
  }
}
```

Comandos utiles:

```bash
curl http://localhost:8001/api/v2/heartbeat
curl -X POST http://localhost:5000/api/admin/vector/reindex
curl -X POST http://localhost:5000/api/public/semantic-search \
  -H "Content-Type: application/json" \
  -d '{"query":"quiero ir a Provenza","limit":5}'
```

Si ChromaDB no esta disponible, la busqueda usa fallback por nombre, codigo o sector en PostgreSQL y devuelve una respuesta clara sin romper el flujo.
