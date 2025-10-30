# Runbook: Connect a New Site to Ingestion

Prereqs

- Event Hubs namespace + hub name
- Network connectivity to OPC UA/MQTT/PI endpoints
- Certificates for mTLS if required

Steps

1. Define tags
   - Edit `config/tags.yaml` and add tag entries with `tagId`, `unit`, and optional `deadband`, `scaleFactor`, `scaleOffset`.
   - Save file; the worker hot-reloads within ~5s.
2. Configure connectors
   - Update `src/Ingestion/appsettings.json` with connection details (OPC UA endpoint URL, MQTT broker, PI server API). (Placeholders for now.)
3. Start ingestion worker
   - `dotnet run --project src/Ingestion/Ingestion.Worker.csproj`
   - Verify logs show connectors started and tag registry loaded.
4. Validate flow
   - Confirm events landing in Event Hubs: use `Azure.Messaging.EventHubs` sample consumer or ADX ingestion monitor.
5. QoS tuning
   - Adjust `deadband` per tag, verify drop/error metrics and lag are acceptable.
6. Security
   - Configure client certs and enable mTLS at brokers/servers; rotate as per policy.

Troubleshooting

- No events: check tagId matches config; verify connector auth and network.
- High lag: increase batching or scale out worker instances; review backpressure.
