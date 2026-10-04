# Universe Server Launcher

Self-contained Windows x64 launcher for the Universe Civilization project.

The launcher:

1. Verifies Node.js and npm.
2. Runs `npm install`.
3. Builds the React/Vite frontend.
4. Builds the TypeScript backend.
5. Starts the backend on port 5001.
6. Starts the Vite frontend on port 3000.
7. Stops both child processes when the launcher exits.

## Local database

Before building, the launcher locates the repository root and performs a PostgreSQL TCP preflight using DATABASE_URL or LOCAL_DATABASE_URL. If PostgreSQL is unreachable, it stops with the host/port and concrete PowerShell diagnostic to run. The launcher does not create PostgreSQL databases or invent credentials.

Configure `.env` first:

```env
DATABASE_URL=postgresql://postgres:YOUR_PASSWORD@localhost:5432/universe_civilization
PORT=5001
NODE_ENV=development
SESSION_SECRET=replace-with-a-long-random-secret
```

The backend reports the sanitized database target and PostgreSQL error code when the database cannot be reached.

## Build

```powershell
dotnet publish tools/UniverseServerLauncher/UniverseServerLauncher.csproj -c Release
```

Output:

`tools/UniverseServerLauncher/bin/Release/net8.0-windows/win-x64/publish/UniverseServerLauncher.exe`
