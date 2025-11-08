using DbUp;
using DbUp.Engine;
using DbUp.Engine.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var configuration = builder.Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? args.FirstOrDefault(a => a.StartsWith("--connection-string="))?.Split('=')[1];

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Connection string required. Use --connection-string=... or CONNECTION_STRING env var.");
    Environment.Exit(1);
}

var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
var scriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");

Console.WriteLine($"Environment: {environment}");
Console.WriteLine($"Scripts path: {scriptsPath}");
Console.WriteLine($"Connecting to: {new Uri(connectionString).Host}");

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsFromFileSystem(scriptsPath)
    .WithTransaction()
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Migration failed: {result.Error}");
    Console.ResetColor();
    Environment.Exit(1);
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Migration successful!");
Console.ResetColor();

// Output executed scripts
foreach (var script in result.Scripts)
{
    Console.WriteLine($"  ✓ {script.Name}");
}

