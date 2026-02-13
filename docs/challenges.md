# NexusSentinel Project Challenges & Solutions

This document chronicles the technical hurdles encountered during the development of NexusSentinel and the engineering decisions made to overcome them.

---

## 1. Blazor Server Interactivity & "Frozen" UI
**Challenge:** The Dashboard would load with a static "Loading..." state but never actually refresh data, even though backend logs confirmed the data was available in Redis.

**Root Cause:** 
- **Prerendering Conflict:** In .NET 8 Blazor, `prerender: true` (default) renders a static HTML snapshot on the server. If the SignalR interactive tunnel fails to establish immediately, the user is left with a non-functional shell.
- **Blocking I/O:** Using synchronous calls for Redis `SCAN` operations on the UI thread was causing intermittent deadlocks.

**Solution:**
- **Disabled Prerendering:** Set `@rendermode @(new InteractiveServerRenderMode(prerender: false))` in `Dashboard.razor`. This ensures the UI only renders once the SignalR connection is active.
- **Asynchronous Pattern:** Offloaded Redis key scanning to `Task.Run()` and utilized `CancellationTokenSource` with a 2-second timeout to prevent UI hangs.

---

## 2. Distributed SignalR Connectivity (Microservices)
**Challenge:** Establishing a stable WebSocket connection between the **Blazor Dashboard** (Client) and the **Notification Service** (Hub) across Docker containers.

**Root Cause:**
- **CORS Misconfiguration:** SignalR requires specific CORS policies (e.g., `AllowCredentials()`) because it manages connection IDs via headers/cookies. 
- **Docker DNS Resolution:** Containers were attempting to reach each other via `localhost`, which refers to the container itself rather than the target service.

**Solution:**
- **Dynamic Hub URLs:** Implemented environment Variable overrides in `docker-compose.yml`. Local development uses `localhost:5072`, while Docker uses the service name `http://notification:8080`.
- **Strict-but-Functional CORS:** Configured the Notification Service to allow credentials while using `SetIsOriginAllowed` to handle the dynamic origin nature of Docker networks.

---

## 3. Kafka Deserialization Mismatch (JSON vs. Protobuf)
**Challenge:** The Alert Processor would crash with an "Input ended unexpectedly" exception when attempting to read from the Kafka `telemetry` topic.

**Root Cause:**
- **Serialization Conflict:** The Ingestion Service was producing messages as raw **JSON strings**, but the Alert Processor was configured to consume them as **Binary Protobuf** messages. The Protobuf parser interpreted the JSON braces as corrupt binary data.

**Solution:**
- **Contract Standardization:** Refactored the internal messaging logic to strictly use Protobuf across the entire pipeline. 
- **Schema Shared Library:** Centralized `.proto` definitions in a `NexusSentinel.Shared` project to ensure all services use identical binary schemas.

---

## 4. Elasticsearch Version Incompatibility (v8 vs. v9)
**Challenge:** The Persistence Service failed to index records, throwing an exception: `Accept version must be either version 8 or 7, but found 9`.

**Root Cause:**
- **Version Skew:** The `Elastic.Clients.Elasticsearch` NuGet package (v8.x) expects compatible headers from the server. Using an experimental v9.x client with a v8.x server caused a protocol rejection.

**Solution:**
- **Version Alignment:** Downgraded the client library to **8.x.x** to perfectly match the Docker image version of Elasticsearch. This fixed the handshake and enabled successful telemetry indexing.

---

## 5. Docker Build Performance & Context Bloat
**Challenge:** Initial Docker build times for C# services exceeded 10-15 minutes, with the context transfer phase being extremely slow.

**Root Cause:**
- **Unfiltered Context:** Docker was copying the entire `bin/`, `obj/`, and `.git/` directories into the build daemon for every service.

**Solution:**
- **Optimization via `.dockerignore`:** Implemented a standardized `.dockerignore` file across all projects. By excluding local build artifacts and IDE metadata, build context size was reduced from hundreds of megabytes to mere kilobytes, slashing build times by over 90%.

---

## 6. Real-time Feedback Loop Strategy
**Challenge:** Deciding between **Blazor Server** and **Vanilla JS** for the monitoring dashboard (Watchtower).

**Root Cause:** 
- Blazor Server is powerful but opaque when debugging low-level WebSocket handshakes.
- Vanilla JS provides "raw" visibility into the browser's network tab, making it easier to diagnose CORS/handshake issues during development.

**Solution:**
- **Hybrid Approach:** The primary **Dashboard** remains in Blazor Server for its ease of integration with C# backends (Redis). A secondary, lightweight **Watchtower** was built using Vanilla JS to serve as a "Network Diagnostic" tool and high-visibility status board.
