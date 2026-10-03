using System.Text;
using System.Text.Json;

const string DefaultBaseUrl = "http://127.0.0.1:3000";
var baseUrl = Environment.GetEnvironmentVariable("UNIVERSE_ADMIN_URL") ?? DefaultBaseUrl;
var session = Environment.GetEnvironmentVariable("UNIVERSE_ADMIN_SESSION");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
http.DefaultRequestHeaders.UserAgent.ParseAdd("UniverseAdminTerminal/1.0");
if (!string.IsNullOrWhiteSpace(session))
    http.DefaultRequestHeaders.Add("Cookie", session);

Console.Title = "Universe Civilization - Admin Systems Terminal";
while (true)
{
    Console.Clear();
    Header();
    Console.WriteLine("  1) System Dashboard");
    Console.WriteLine("  2) Player Operations");
    Console.WriteLine("  3) Security & Audit");
    Console.WriteLine("  4) Universe Control");
    Console.WriteLine("  5) Maintenance");
    Console.WriteLine("  6) Backend Menu");
    Console.WriteLine("  7) Command");
    Console.WriteLine("  8) Connection");
    Console.WriteLine("  0) Exit");
    Console.Write("\nADMIN> ");
    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "1": await Execute("status"); break;
            case "2": await PlayerMenu(); break;
            case "3": await Execute("audit recent", new { limit = 25 }); break;
            case "4": await ConfigMenu(); break;
            case "5": await Execute("queues"); break;
            case "6": await Execute("help"); break;
            case "7": await CommandMenu(); break;
            case "8": await ConnectionMenu(); break;
            case "0": return;
            default: Pause("Invalid menu selection."); break;
        }
    }
    catch (Exception ex)
    {
        Pause("ERROR: " + ex.Message);
    }
}

void Header()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  UNIVERSE CIVILIZATION — ADMIN SYSTEMS TERMINAL        ║");
    Console.WriteLine("║  Server-authoritative administrative console            ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine($"  Endpoint: {baseUrl}");
    Console.WriteLine();
}

async Task PlayerMenu()
{
    Console.Clear(); Header();
    Console.WriteLine("  PLAYER OPERATIONS");
    Console.WriteLine("  1) Search users");
    Console.WriteLine("  2) Ban user");
    Console.WriteLine("  3) Unban user");
    Console.WriteLine("  0) Back");
    Console.Write("\nPLAYER> ");
    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            Console.Write("Search query> ");
            await Execute("users search", new { query = Console.ReadLine() ?? "" }); break;
        case "2":
            Console.Write("User ID> ");
            await Execute("users ban", new { userId = Console.ReadLine() ?? "" }); break;
        case "3":
            Console.Write("User ID> ");
            await Execute("users unban", new { userId = Console.ReadLine() ?? "" }); break;
    }
}

async Task ConfigMenu()
{
    Console.Clear(); Header();
    Console.WriteLine("  UNIVERSE CONTROL");
    Console.WriteLine("  1) Get configuration");
    Console.WriteLine("  2) Set configuration");
    Console.WriteLine("  0) Back");
    Console.Write("\nCONFIG> ");
    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            Console.Write("Config key> ");
            await Execute("config get", new { key = Console.ReadLine() ?? "" }); break;
        case "2":
            Console.Write("Config key> ");
            var key = Console.ReadLine() ?? "";
            Console.Write("Config value> ");
            var value = Console.ReadLine() ?? "";
            await Execute("config set", new { key, value }); break;
    }
}

async Task CommandMenu()
{
    Console.Clear(); Header();
    Console.WriteLine("Enter an allow-listed backend command.");
    Console.WriteLine("Examples: status, users search, audit recent, config get, queues, db health, help");
    Console.Write("COMMAND> ");
    var command = Console.ReadLine()?.Trim();
    if (!string.IsNullOrWhiteSpace(command)) await Execute(command);
}

async Task ConnectionMenu()
{
    Console.Clear(); Header();
    Console.WriteLine("Authentication is supplied by UNIVERSE_ADMIN_SESSION.");
    Console.WriteLine("Session configured: " + !string.IsNullOrWhiteSpace(session));
    Console.WriteLine("Server: " + baseUrl);
    await Execute("status");
}

async Task Execute(string command, object? args = null)
{
    var payload = JsonSerializer.Serialize(new { command, args = args ?? new { } });
    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
    using var response = await http.PostAsync("api/admin/terminal/execute", content);
    var body = await response.Content.ReadAsStringAsync();

    Console.WriteLine();
    Console.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
    try
    {
        using var doc = JsonDocument.Parse(body);
        Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch
    {
        Console.WriteLine(body);
    }
    Pause();
}

void Pause(string? message = null)
{
    if (!string.IsNullOrWhiteSpace(message)) Console.WriteLine(message);
    Console.WriteLine("\nPress Enter to continue...");
    Console.ReadLine();
}
