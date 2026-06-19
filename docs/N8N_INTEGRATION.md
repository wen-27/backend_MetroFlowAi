# Integracion n8n

n8n actua como orquestador HTTP. No se usan nodos de IA ni chat generativo en el MVP.

## Webhook esperado

```json
{
  "sessionId": "opcional",
  "selectedOption": 1,
  "action": "assistant-step"
}
```

## Flujo recomendado

1. `Webhook` recibe evento del canal externo.
2. `Switch/IF` decide si inicia o continua una sesion.
3. `HTTP Request` llama `POST /api/assistant/start` o `POST /api/assistant/step`.
4. `Set/Code` adapta `message`, `options` y `data` al canal.

## Endpoints utiles

- Iniciar chat: `POST http://host.docker.internal:5000/api/assistant/start`
- Enviar opcion: `POST http://host.docker.internal:5000/api/assistant/step`
- Alertas: `GET http://host.docker.internal:5000/api/public/alerts`
- ETA: `GET http://host.docker.internal:5000/api/public/stations/{stationId}/arrivals`
- Mejor ruta: `POST http://host.docker.internal:5000/api/public/route-plan`
- Reindexar vectorial: `POST http://host.docker.internal:5000/api/admin/vector/reindex`
- Dashboard: `GET http://host.docker.internal:5000/api/admin/dashboard`
