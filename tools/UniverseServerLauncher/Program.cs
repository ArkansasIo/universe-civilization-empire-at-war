using System.Diagnostics;

namespace UniverseServerLauncher;

internal static class Program
{
    private static readonly string Root = AppContext.BaseDirectory;

    private static int Main()
    {
        Console.Title = "Universe Civilization — Server Launcher";
        PrintHeader();

        if (!RunCheck("node", "--version", "Node.js")) return 1;
        if (!RunCheck("npm", "--version", "npm")) return 1;

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
            WorkingDirectory = Root,
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
