# NexusSentinel - Living Architecture Document

**Status:** Active Development  
**Last Updated:** 2026-02-05

---

## 1. Technical Strategy & Decisions

### 1.1 Messaging & Event Streaming
We deliberately distinguish between High-Throughput streaming and Reliable Messaging to learn both patterns.

*   **Kafka (Telemetry Pipeline):**
    *   **Role:** High-throughput data ingestion for IoT telemetry (Speed > Absolute Reliability).
    *   **Pattern:** Fire-and-forget, Stream Processing.
    *   **Retention:** Logs are kept for replayability/debugging.
*   **RabbitMQ (Operational Events):**
    *   **Role:** Critical Alerts, System Commands, Control Plane (Reliability > Speed).
    *   **Pattern:** Message Queuing, Routing, Confirmation (Ack/Nack).
    *   **Guarantee:** At-least-once delivery is required here.

### 1.2 Future Roadmap (Polyglot & Extensibility)
The architecture is designed to be language-agnostic. We define contracts first.

*   **GoLang Migration:** The "Ingestion Service" is a candidate to be rewritten in Go in the future for raw socket performance.
*   **Frontend Diversity:** While the Admin Dashboard is Blazor, the backend APIs must support future React/Vue portals for end-customers.

---

## 2. High-Level Architecture
*(Waiting for User Approval to populate this section with Diagram/Explanation)*
