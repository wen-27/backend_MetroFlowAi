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

## Asistente

```http
POST /api/assistant/start
POST /api/assistant/step
```

```json
{
  "sessionId": "session-key",
  "selectedOption": 1
}
```
