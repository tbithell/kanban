using System.Data;
using System.Reflection;
using DbUp;
using Kanban.DataAccess;
using Microsoft.Data.Sqlite;

namespace Kanban.Tests.Integration.Infrastructure;

public sealed class SqliteTestFixture : IAsyncLifetime
{
    private SqliteConnection? _anchor;

    public string ConnectionString { get; } =
        $"Data Source=kanban_test_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public async Task InitializeAsync()
    {
        DapperConfiguration.RegisterTypeHandlers();

        _anchor = new SqliteConnection(ConnectionString);
        await _anchor.OpenAsync();

        var dataAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Kanban.Data")
            ?? Assembly.Load("Kanban.Data");

        var result = DeployChanges.To
            .SQLiteDatabase(ConnectionString)
            .WithScriptsEmbeddedInAssembly(
                dataAssembly,
                name => name.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
            .WithVariables(new Dictionary<string, string>
            {
                ["AdminUserId"] = Guid.NewGuid().ToString("D"),
                ["AdminEmail"] = "admin@test.local",
                ["SeedTimestamp"] = DateTimeOffset.UtcNow.ToString("o"),
            })
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException($"DbUp migration failed: {result.Error?.Message}");
    }

    public Task DisposeAsync()
    {
        _anchor?.Dispose();
        return Task.CompletedTask;
    }
}
