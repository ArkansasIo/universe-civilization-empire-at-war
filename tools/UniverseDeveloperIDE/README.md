# Universe Developer IDE

Native Windows desktop development tool for the repository.

## Features

- Recursive workspace/file explorer.
- Editable source files for frontend and backend.
- Open/save/save-all.
- File filtering and search.
- Frontend menu: npm install, dev, build, lint, dependency test.
- Backend menu: server build/start/dev/compile-run.
- Database and migration shortcuts.
- Git status/log tools.
- EXE publishing tools.
- Integrated command output panel.
- Windows terminal and Explorer integration.

## Supported project areas

- `src/` frontend
- `server/` backend
- `shared/` shared TypeScript
- `database/` SQL schema and migrations
- `tools/` developer/admin tools
- `.github/` CI/CD
- Root configuration files such as package.json and tsconfig files

## Build

```powershell
dotnet publish tools/UniverseDeveloperIDE/UniverseDeveloperIDE.csproj -c Release
```

Output:

`tools/UniverseDeveloperIDE/bin/Release/net8.0-windows/win-x64/publish/UniverseDeveloperIDE.exe`

The executable is self-contained and does not require the .NET runtime on the target Windows machine.

The IDE intentionally operates on the selected local workspace. It does not embed GitHub credentials or bypass repository permissions.
