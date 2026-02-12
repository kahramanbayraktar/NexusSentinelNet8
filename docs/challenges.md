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

## 3. NETSDK1152 - Duplicate Publish Output Files (Project Reference Conflict)

### **Problem Symptom**
Docker build for the Watchtower service failed with the following error:
`error NETSDK1152: Found multiple publish output files with the same relative path: appsettings.json, appsettings.Development.json.`

### **Root Cause**
- **The Mistake:** The `NexusSentinel.Watchtower.csproj` file contained an unnecessary `<ProjectReference>` to `NexusSentinel.Notification.csproj`.
- **The Mechanics:** Both projects are independent ASP.NET Core applications and both have their own `appsettings.json` files. When `dotnet publish` was run for Watchtower, it recursively attempted to include the output of all referenced projects. Since both projects have files with the exact same name and relative path, the build engine encountered a collision.
- **Source of Error:** This reference was likely added incorrectly during an AI-assisted code generation or autocomplete session where a shared dependency was needed, but the AI suggested the wrong project.

### **Solution Implemented**
- **Reference Cleanup:** Removed the reference to the `Notification` project and replaced it with a reference to the `NexusSentinel.Shared` project, which was the actual intended dependency.
- **Result:** The build succeeded as there were no longer conflicting configuration files in the publish output.

### **Key Lessons Learned**
- **Audit Project References:** Be extremely careful with Project References between different service projects in a microservice architecture. Services should typically only share "Shared" or "Domain" libraries, not reference each other directly.
- **AI Verification:** Always double-check AI-generated `.csproj` entries or boilerplate code, as it might introduce broad dependencies that cause subtle build-time conflicts.

## 4. SignalR Hub Connection Failures (Blazor vs Vanilla JS)

### **Problem Symptom**
Persistent "Connection closed with an error" or WebSocket handshake failures in the Blazor-based Watchtower project, both inside and outside of Docker.

### **Root Causes**
- **Lifecycle & Prerendering:** Blazor Server's complex lifecycle and default prerendering often conflict with SignalR connection timing, especially when external services are involved.
- **Opaque Errors:** Blazor abstracts much of the SignalR handshake, making it difficult to debug low-level issues like CORS or Protocol mismatches compared to the browser console's direct feedback with JavaScript.

### **Solution Implemented**
- **Technology Pivot:** Shifted from Blazor to a lightweight **Vanilla HTML/JS + CSS** approach for the Watchtower UI.
- **Benefit:** This provided direct access to the browser's developer tools (F12), making it easier to identify and fix CORS and connection issues in real-time.

## 5. Docker Networking vs. Localhost Confusion (Connection Refused)

### **Problem Symptom**
The Watchtower UI could not connect to the Notification service from within a container, even though it worked locally.

### **Root Cause**
- **Localhost Trap:** Within a Docker container, `localhost` refers to the container itself, not the host machine or other containers. 
- **Port Mapping Confusion:** Misunderstanding that `ports: 5072:8080` allows the *Host* to reach the container on 5072, but container-to-container communication must happen via the service name and internal port (e.g., `http://notification:8080`).

### **Solution Implemented**
- **Environment Variables:** Used .NET configuration to store the Hub URL and overrode it in `docker-compose.yml` with the correct Docker internal DNS (`notification:8080`).
- **Hybrid Support:** Kept `localhost:5072` in `appsettings.json` for IDE development and used environment variables for Docker deployment.
