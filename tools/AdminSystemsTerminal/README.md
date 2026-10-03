# Universe Admin Systems Terminal — Windows EXE

Native Windows console client for the Universe Civilization admin terminal.

## Build

Requires the .NET 8 SDK.

From the repository root:

    dotnet publish tools/AdminSystemsTerminal/AdminSystemsTerminal.csproj -c Release

The self-contained single-file executable is emitted under:

    tools/AdminSystemsTerminal/bin/Release/net8.0-windows/win-x64/publish/UniverseAdminTerminal.exe

## Run

Set the backend URL:

    set UNIVERSE_ADMIN_URL=http://127.0.0.1:3000

Set the authenticated server session cookie:

    set UNIVERSE_ADMIN_SESSION=connect.sid=YOUR_SESSION_COOKIE

Then launch UniverseAdminTerminal.exe.

The executable does not store an administrator password and does not bypass the server RBAC layer. Every command is sent to the existing /api/admin/terminal/execute endpoint and is authorized by the backend.

For production, use HTTPS and a short-lived authenticated session. Do not put credentials directly in source code or command-line history.
