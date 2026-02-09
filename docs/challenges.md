# NexusSentinel Project Challenges & Solutions

## 1. Blazor Dashboard Infinite Loading & Refresh Failure

### **Problem Symptom**
The Blazor Dashboard UI would initialize with a "Loading..." spinner but would never refresh or display real-time data. To the user, it appeared as if the application was "frozen" or unable to reach the backend, even though logs showed the backend services were processing data correctly.

### **Root Causes & Debunked Theories**

1.  **CONFIRMED: Render Mode & Prerendering:**
    In .NET 8 Blazor, pages must be explicitly set to `@rendermode InteractiveServer`. However, the critical fix was setting **`prerender: false`**.
    *   *The Trap:* With `prerender: true` (the default), Blazor renders a static "snapshot" of the page on the server first. The user sees the UI, but it's just a non-interactive HTML shell. If the SignalR connection (which makes the page "alive") fails to establish due to environment issues, the page stays in this static state forever.
    *   *The Solution:* Setting `@rendermode @(new InteractiveServerRenderMode(prerender: false))` forced the page to wait for a successful SignalR connection before rendering the content. This eliminated the "frozen shell" and allowed the data-refresh loop to function correctly only after the interactive tunnel was truly open.

2.  **DEBUNKED: Blocking Redis Calls:**
    We initially suspected that the synchronous `_server.Keys(...).ToArray()` call was deadlocking the UI thread.
    *   *Test Result:* We reverted the code to the synchronous version and the Dashboard **still worked perfectly**. This proves that while async/await is a best practice, the synchronous call was not responsible for the complete freeze.

3.  **POTENTIAL: Environment & Cache Confusion:**
    Running the dashboard on different ports (5003 in Docker, 5008 locally) and switching between containers might have led to browser cache or SignalR negotiation issues.
    *   *Result:* This might explain why some changes seemed to not reflect immediately during testing.

### **Solutions Implemented**

- **Explicit Render Mode:** Added `@rendermode @(new InteractiveServerRenderMode(prerender: false))` to the `Dashboard.razor`. Disabling prerendering helped isolate SignalR connection issues immediately.
- **Asynchronous Offloading:** Wrapped the blocking `Keys` command in `Task.Run(() => ...)` to ensure the UI thread remains free.
- **Defensive Timeout Privacy:** Implemented a `CancellationTokenSource` with a 2-second timeout in the data-refresh loop. This ensures that if Redis hangs, the user sees an error message instead of an infinite spinner.
- **Port Isolation:** Stopped Docker containers and ran the service locally via `dotnet run` to bypass Docker's internal networking complexities until the core logic was verified.

### **Key Lessons Learned**
- **Always Verify Interactivity:** In .NET 8+, if a component needs a timer or event handlers (clicks, etc. to work after initial load), ensure `@rendermode` is set.
- **Async-First mentality:** Never use blocking `.ToArray()` or `.ToList()` on I/O-bound resources (like Redis `SCAN`) inside a UI application context without offloading them.
- **Feedback is Better than Silence:** Using `try-catch` blocks that update an `ErrorMessage` property on the UI is far superior to letting an app hang silently.

## 2. Notification Service SignalR Connection Failure (SKIPPED)

### **Problem Symptom**
We attempted to implement real-time alerts on the Dashboard by connecting it to the `NotificationHub` via SignalR. However, we faced persistent issues establishing a stable connection between the Blazor Server app (Dashboard) and the Notification Service running in a separate container/process.

### **Root Causes & Unknowns**
-   **Potential Docker Networking:** The Dashboard could not consistently reach the Notification Service's SignalR hub endpoint.
-   **Configuration Complexity:** Managing multiple SignalR hubs (one for internal updates, one for external notifications) added complexity.

### **Action Taken**
-   **Decision:** We decided to **SKIP** the real-time alert notification feature for now to prevent blocking overall project progress.
-   **Plan:** We will revisit this later, possibly exploring alternative communication methods (e.g., polling Redis for alerts, or using a different event bus strategy) or dedicating more time to debug the specific SignalR networking issue.
