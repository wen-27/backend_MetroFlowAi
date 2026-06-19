# Graph Routing

Cada estacion es un nodo y cada `RouteSegment` es una arista bidireccional para la demo.

El peso se calcula asi:

```text
peso = tiempo_base_minutos
     + penalizacion_por_ocupacion
     + penalizacion_por_incidente
     + penalizacion_por_retraso
     + penalizacion_por_congestion
```

`RoutePlannerService` implementa Dijkstra sobre el grafo cacheado por `RouteGraphService`. El cache se invalida al crear o resolver incidentes y con `POST /api/admin/graph/rebuild`.

