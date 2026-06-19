# Integracion n8n

n8n actua como orquestador HTTP para integraciones externas. No se usan nodos de IA ni chat generativo en el MVP.

El chatbox guiado numerico lo controla el frontend. n8n puede consultar datos o disparar tareas operativas, pero no guarda el estado del chatbox ni decide el siguiente paso del flujo.

## Webhook esperado

```json
{
  "action": "route-plan",
  "originStationId": "guid",
  "destinationStationId": "guid"
}
```

## Flujo recomendado

1. `Webhook` recibe evento del canal externo.
2. `Switch/IF` decide si consulta alertas, ETA, mejor ruta, dashboard o reindexacion.
3. `HTTP Request` llama el endpoint backend correspondiente.
4. `Set/Code` adapta la respuesta al canal externo.

## Endpoints utiles

- Alertas: `GET http://host.docker.internal:5000/api/public/alerts`
- ETA: `GET http://host.docker.internal:5000/api/public/stations/{stationId}/arrivals`
- Mejor ruta: `POST http://host.docker.internal:5000/api/public/route-plan`
- Reindexar vectorial: `POST http://host.docker.internal:5000/api/admin/vector/reindex`
- Dashboard: `GET http://host.docker.internal:5000/api/admin/dashboard`
