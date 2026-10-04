using System.Diagnostics;

namespace UniverseServerLauncher;

internal static class Program
{
    private static string? ProjectRoot;

    private static int Main()
    {
        Console.Title = "Universe Civilization — Server Launcher";
        PrintHeader();

        if (!RunCheck("node", "--version", "Node.js")) return 1;
        if (!RunCheck("npm", "--version", "npm")) return 1;

        if (!CheckPostgreSql()) return 1;

        Console.WriteLine("[1/5] Installing/verifying npm dependencies...");
        if (!Run("npm", "install")) return 1;

        Console.WriteLine("[2/5] Building frontend...");
        if (!Run("npm", "run build")) return 1;

        Console.WriteLine("[3/5] Building backend...");
        if (!Run("npm", "run server:build")) return 1;

        Console.WriteLine("[4/5] Starting backend on http://localhost:5001/ ...");
        var backend = Start("node", "dist-server/index.js");

        Console.WriteLine("[5/5] Starting frontend Vite server on http://localhost:3000/ ...");
        var frontend = Start("npm", "run dev -- --host 0.0.0.0 --port 3000");

        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine(" UNIVERSE CIVILIZATION — RUNNING");
        Console.WriteLine(" Frontend: http://localhost:3000/");
        Console.WriteLine(" Backend:  http://localhost:5001/");
        Console.WriteLine(" Health:   http://localhost:5001/api/status/health");
        Console.WriteLine("============================================================");
        Console.WriteLine("Press Ctrl+C to stop both servers.");

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Stop(backend, frontend);

        while (true)
        {
            if (backend.HasExited)
            {
                Console.WriteLine($"[ERROR] Backend exited with code {backend.ExitCode}.");
                Stop(frontend);
                return backend.ExitCode == 0 ? 0 : 1;
            }

            if (frontend.HasExited)
            {
                Console.WriteLine($"[ERROR] Frontend exited with code {frontend.ExitCode}.");
                Stop(backend);
                return frontend.ExitCode == 0 ? 0 : 1;
            }

            Thread.Sleep(1000);
        }
    }



    private static string? FindProjectRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "package.json")) &&
                    File.Exists(Path.Combine(directory.FullName, "server", "index.ts")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }
        return null;
    }

    private static bool CheckPostgreSql()
    {
        var url = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("LOCAL_DATABASE_URL")
            ?? ReadEnv("DATABASE_URL")
            ?? ReadEnv("LOCAL_DATABASE_URL")
            ?? "postgresql://postgres@localhost:5432/universe_civilization";

        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            var port = uri.IsDefaultPort ? 5432 : uri.Port;
            var database = uri.AbsolutePath.Trim('/').Length > 0 ? uri.AbsolutePath.Trim('/') : "postgres";
            var user = uri.UserInfo.Split(':', 2)[0];
            if (string.IsNullOrWhiteSpace(user)) user = "postgres";

            Console.WriteLine($"[0/5] PostgreSQL preflight: {user}@{host}:{port}/{database}");

            using var tcp = new System.Net.Sockets.TcpClient();
            var task = tcp.ConnectAsync(host, port);
            if (!task.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("connection timed out");

            Console.WriteLine("       PostgreSQL TCP port is reachable.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] PostgreSQL is not reachable: {ex.Message}");
            Console.WriteLine("       Check that the PostgreSQL service is running and DATABASE_URL is correct.");
            Console.WriteLine("       PowerShell: Test-NetConnection localhost -Port 5432");
            Console.WriteLine("       The launcher will not invent credentials or create databases.");
            return false;
        }
    }

    private static string? ReadEnv(string key)
    {
        if (ProjectRoot is null) return null;
        var path = Path.Combine(ProjectRoot, ".env");
        if (!File.Exists(path)) return null;

        foreach (var line in File.ReadLines(path))
        {
            var value = line.Trim();
            if (value.Length == 0 || value.StartsWith('#')) continue;
            var separator = value.IndexOf('=');
            if (separator <= 0) continue;
            if (!string.Equals(value[..separator].Trim(), key, StringComparison.Ordinal)) continue;
            return value[(separator + 1)..].Trim().Trim('"', '\'');
        }
        return null;
    }

    private static void PrintHeader()
    {
        Console.WriteLine("============================================================");
        Console.WriteLine(" UNIVERSE CIVILIZATION — WINDOWS SERVER LAUNCHER");
        Console.WriteLine(" Frontend + Backend + Dependency Setup");
        Console.WriteLine("============================================================");
        Console.WriteLine();
    }

    private static bool RunCheck(string command, string args, string name)
    {
        Console.WriteLine($"Checking {name}...");
        if (!Run(command, args))
        {
            Console.WriteLine($"[ERROR] {name} is missing or not available in PATH.");
            Console.WriteLine("Install Node.js LTS, reopen the terminal, and run this launcher again.");
            return false;
        }

        return true;
    }

    private static bool Run(string command, string args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            WorkingDirectory = ProjectRoot!,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false,
        };

        try
        {
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Could not run {command}: {ex.Message}");
            return false;
        }
    }

    private static Process Start(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = Root,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false,
            }
        };

        process.Start();
        return process;
    }

    private static void Stop(params Process[] processes)
    {
        foreach (var process in processes)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort shutdown.
            }
        }
    }
}
