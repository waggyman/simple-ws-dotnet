# Simple WebSocket Server

Repository: [github.com/waggyman/simple-ws-dotnet](https://github.com/waggyman/simple-ws-dotnet)

A small ASP.NET Core WebSocket server built as a tech assignment sample. It exposes one WebSocket endpoint with a JSON message protocol, plus basic HTTP routes for discovery and health checks.

Built with ASP.NET Core 10 and `System.Net.WebSockets`.

## What it does

- **WebSocket** — connect to `/ws`, send JSON messages with an `action` field (`echo`, `ping`, `broadcast`).
- **Health check** — `GET /health` is public and reports status plus active connection count.
- **Discovery** — `GET /` returns basic server info and endpoint paths.

Default URL when running locally: **http://localhost:5082**

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) (includes the `dotnet` CLI).

To build a standalone app that runs **without** installing the SDK, see [Build a standalone app](#build-a-standalone-app) below. End users only need the published binary, not the SDK.

Optional:

- **Git** — to clone the repository
- **Docker** — to run via container (see [Docker](#docker-optional))

## Clone and install

```bash
git clone https://github.com/waggyman/simple-ws-dotnet.git
cd simple-ws-dotnet
dotnet restore
```

This downloads NuGet packages for the test project. You only need to run `dotnet restore` once after cloning (or when dependencies change). The main app has no extra NuGet dependencies — only the .NET SDK is required to run it.

## Local setup

1. **Port** — HTTP listens on port **5082** (see `Properties/launchSettings.json`).

2. **WebSocket path** — default is `/ws` (see `appsettings.json`, key `WebSocket:Path`).

No database or secrets are required for local development.

## Run locally (with SDK)

From the project root:

```bash
dotnet run --launch-profile http
```

Check that it works:

```bash
curl http://localhost:5082/health
```

Expected response:

```json
{"status":"ok","activeConnections":0,"webSocketPath":"/ws"}
```

Test the WebSocket in Postman (`ws://localhost:5082/ws`) or any WebSocket client.

### Auto-restart on file changes

```bash
chmod +x watch.sh
./watch.sh
```

On Linux, if `dotnet watch` fails with an inotify limit error, the script enables polling. Alternatively:

```bash
DOTNET_USE_POLLING_FILE_WATCHER=true dotnet watch run --launch-profile http
```

### Tests

```bash
dotnet test
```

## Project structure

```
simple-ws-dotnet/
├── Program.cs                  # App entry point, route mapping, DI setup
├── appsettings.json            # Config (WebSocket path, logging)
├── Models/
│   └── WsMessage.cs            # Request/response message shapes
├── Services/
│   ├── IMessageHandler.cs      # Message handling contract
│   └── MessageHandler.cs       # echo / ping logic
├── WebSocket/
│   ├── ConnectionManager.cs    # Tracks active connections
│   └── WebSocketHandler.cs     # Accepts connections, read/write loop
├── tests/
│   └── simple-ws-dotnet.Tests/ # Integration tests (HTTP + WebSocket)
├── watch.sh                    # dotnet watch helper (Linux)
├── Dockerfile                  # Container build
└── Properties/
    └── launchSettings.json     # Local dev URLs
```

| Folder | Role |
|--------|------|
| `Models` | JSON message shapes |
| `Services` | Message rules (`echo`, `ping`) |
| `WebSocket` | Connection tracking and I/O |
| `tests` | Automated integration tests |

## WebSocket protocol

Connect to:

```
ws://localhost:5082/ws
```

Send **text** frames with JSON:

| action | data | server reply |
|--------|------|--------------|
| `echo` | any | same text back to sender, with a timestamp |
| `ping` | — | `{ "action": "pong", ... }` to sender |
| `broadcast` | any | `{ "action": "broadcast", "data": "...", ... }` to **all** connected clients |

Examples:

```json
{ "action": "echo", "data": "hello from postman" }
```

```json
{ "action": "ping" }
```

```json
{ "action": "broadcast", "data": "hello everyone" }
```

Invalid JSON or unknown actions return:

```json
{ "action": "error", "data": "...", "timestamp": "..." }
```

## Testing with Postman

1. Start the server: `dotnet run --launch-profile http`
2. In Postman, create a **New → WebSocket Request**
3. URL: `ws://localhost:5082/ws`
4. Click **Connect**
5. In the message box, send:

   ```json
   { "action": "ping" }
   ```

   You should see a `pong` response in the messages panel.

6. Try echo:

   ```json
   { "action": "echo", "data": "test message" }
   ```

7. Open a second WebSocket tab and connect again — `GET /health` should show `activeConnections: 2`

8. From tab 1, broadcast a message:

   ```json
   { "action": "broadcast", "data": "hello from tab 1" }
   ```

   Both tabs should receive the same `broadcast` message.

## Configuration

| Setting | Purpose |
|---------|---------|
| `WebSocket:Path` | WebSocket endpoint path (default `/ws`) |

Environment variables use `__` instead of `:` (example: `WebSocket__Path=/chat`).

Override via `appsettings.json`, environment variables, or `launchSettings.json`.

## Build a standalone app

You can publish a **self-contained** build that bundles the .NET runtime. On the target machine you run the executable directly — no `dotnet` CLI required.

Publish from the project file (not the `.sln`):

### Linux (x64)

On Linux:

```bash
dotnet publish simple-ws-dotnet.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -o publish/linux-x64
```

Output: `publish/linux-x64/simple-ws-dotnet` (single executable, ~100 MB).

Run it:

```bash
cd publish/linux-x64
export ASPNETCORE_URLS=http://localhost:5082
./simple-ws-dotnet
```

### Windows (x64, `.exe`)

On Windows, or cross-compile from Linux/macOS with the SDK installed:

```bash
dotnet publish simple-ws-dotnet.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o publish/win-x64
```

Output: `publish/win-x64/simple-ws-dotnet.exe`

Run on Windows (Command Prompt or PowerShell):

```cmd
cd publish\win-x64
set ASPNETCORE_URLS=http://localhost:5082
simple-ws-dotnet.exe
```

**Framework-dependent** (smaller, requires .NET runtime on the machine):

```bash
dotnet publish simple-ws-dotnet.csproj -c Release -o ./publish
dotnet ./publish/simple-ws-dotnet.dll
```

### Notes on published builds

- `appsettings.json` is included in the publish folder; override settings with environment variables.
- `PublishSingleFile=true` produces one main binary; first startup may be slightly slower while extracting.
- ARM Mac/Linux: use `-r osx-arm64`, `linux-arm64`, etc., instead of `x64` if needed.

## Docker (optional)

```bash
docker build -t simple-ws-dotnet .
docker run -p 8080:8080 simple-ws-dotnet
```

HTTP: http://localhost:8080  
Health: http://localhost:8080/health  
WebSocket: ws://localhost:8080/ws

Override the WebSocket path:

```bash
docker run -p 8080:8080 -e WebSocket__Path=/chat simple-ws-dotnet
```

## Notes

- Only text WebSocket frames are handled; binary frames get an error reply.
- Connection count is in-memory and resets when the process restarts.
- HTTPS/WSS is not configured out of the box — use a reverse proxy in production if needed.
