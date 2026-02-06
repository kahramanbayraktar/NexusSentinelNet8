# NexusSentinel - Project Roadmap

This document tracks the overall progress of the NexusSentinel distributed IoT monitoring system.

## 🚀 Phase 1: Foundation & Infrastructure (DONE)
- [x] **Project Structure:** Create solution and folder structure (`src`, `docs`, `docker`).
- [x] **Docker Environment:** Set up Kafka, Zookeeper, and Redis via `docker-compose`.
- [x] **Shared Library:** Create `NexusSentinel.Shared` for protobuf contracts and common models.

## 📡 Phase 2: Ingestion Layer (DONE)
- [x] **IoT Simulator:** Create a console app to simulate sensors sending gRPC data.
- [x] **Ingestion Service:** Create a gRPC server (`NexusSentinel.Ingestion`) to receive data.
- [x] **Kafka Producer:** Integrate Ingestion Service to publish data to Kafka (`telemetry` topic).

## ⚙️ Phase 3: Processing Layer (DONE)
- [x] **Processor Service:** Create a Worker Service (`NexusSentinel.Processor`).
- [x] **Containerization:** Add Dockerfile and integrate into specific network.
- [x] **Data Consumption:** Consume messages from Kafka.
- [x] **Storage:** Deserialize JSON and store latest state in Redis.

## 🖥️ Phase 4: Visualization Layer (CURRENT)
- [ ] **Dashboard Project:** Create Blazor Server project (`NexusSentinel.Dashboard`).
- [ ] **Redis Connection:** Read live data from Redis.
- [ ] **UI Components:** Create real-time data grid/cards.
- [ ] **SignalR:** Push updates to UI (Optional/Advanced).

## 🔔 Phase 5: Alerting & Advanced Features (FUTURE)
- [ ] **Alert Processor:** Analyze data for thresholds (e.g., Temp > 50).
- [ ] **Notification:** Send alerts (Email/Simulate).
- [ ] **Historical Data:** Save time-series data to Elasticsearch or TimescaleDB.

## � Phase 6: Search & Analytics (Elasticsearch) (FUTURE)
- [ ] **Data Sink:** Implement a Kafka Connect or custom Consumer to push data to Elasticsearch.
- [ ] **Log Analysis:** Centralized logging with ELK Stack (or similar).
- [ ] **Complex Queries:** Implement full-text search on device logs.

## 🤖 Phase 7: AI/ML Integration (FUTURE)
- [ ] **Anomaly Detection:** Train a model to detect abnormal patterns (e.g., sudden temp spikes).
- [ ] **Predictive Maintenance:** Predict when a device might fail based on vibration trends.
- [ ] **AI Service:** Create a Python/Flask service (or ML.NET) consuming Kafka data for inference.

## �📚 Documentation & Learning
- [x] **Questions:** Maintain `docs/my-questions.md`.
- [x] **Architecture:** Maintain `docs/architecture.md`.
