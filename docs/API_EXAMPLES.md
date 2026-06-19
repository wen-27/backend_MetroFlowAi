# API Examples

## Planear ruta

```http
POST /api/public/route-plan
Content-Type: application/json
```

```json
{
  "originStationId": "00000000-0000-0000-0000-000000000000",
  "destinationStationId": "00000000-0000-0000-0000-000000000000",
  "avoidHighCongestion": true,
  "avoidIncidents": true
}
```

## Busqueda semantica

```json
{
  "query": "quiero ir a Provenza",
  "limit": 5
}
```

## Chatbox guiado desde frontend

El frontend maneja el estado del flujo numerico. Ejemplo:

1. Usuario elige `Consultar mejor ruta`.
2. Frontend llama `GET /api/public/stations`.
3. Usuario selecciona origen y destino.
4. Frontend llama `POST /api/public/route-plan`.
