# ChromaDB

La integracion esta detras de:

- `Application/VectorSearch/Abstractions/IVectorSearchService.cs`
- `Application/VectorSearch/Abstractions/IVectorIndexingService.cs`
- `Infrastructure/VectorSearch/Chroma/ChromaVectorSearchService.cs`
- `Infrastructure/VectorSearch/Chroma/ChromaVectorIndexingService.cs`

`POST /api/admin/vector/reindex` genera documentos de estaciones, rutas, incidentes y recomendaciones.

Si ChromaDB no esta disponible, la busqueda usa fallback por nombre, codigo o sector en MySQL y devuelve una respuesta clara sin romper el flujo.
