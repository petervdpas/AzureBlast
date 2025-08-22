using System;
using System.Linq;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast.Tests;

/// <summary>
/// Unit tests for <see cref="MssqlDatabase"/> using LocalDB.
/// </summary>
public class MssqlDatabaseTests : IDisposable
{
    private const string ConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;Pooling=false;";
    private readonly IMssqlDatabase _db;

    public MssqlDatabaseTests()
    {
        var provider = AzureBlastFactory.CreateServiceProvider(opt =>
        {
            opt.SqlConnectionString = ConnStr;
        });

        _db = provider.GetRequiredService<IMssqlDatabase>();

        _db.ExecuteNonQuery("""
            CREATE TABLE TestTable (
                Id INT PRIMARY KEY IDENTITY,
                Name NVARCHAR(50) NOT NULL,
                Value INT NULL
            );
        """);
    }

    [Fact]
    public void ExecuteInsert_ShouldAffectOneRow()
    {
        var affected = _db.ExecuteInsert("INSERT INTO TestTable (Name, Value) VALUES (@Name, @Value)",
            new() { ["Name"] = "Test", ["Value"] = 123 });

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ExecuteScalar_ShouldReturnInsertedName()
    {
        _db.ExecuteInsert("INSERT INTO TestTable (Name, Value) VALUES (@Name, @Value)",
            new() { ["Name"] = "Alpha", ["Value"] = 5 });

        var result = _db.ExecuteScalar("SELECT Name FROM TestTable WHERE Value = @Value",
            new() { ["Value"] = 5 });

        Assert.Equal("Alpha", result?.ToString());
    }

    [Fact]
    public void ExecuteQuery_ShouldReturnCorrectRow()
    {
        _db.ExecuteInsert("INSERT INTO TestTable (Name, Value) VALUES (@Name, @Value)",
            new() { ["Name"] = "Beta", ["Value"] = 9 });

        var table = _db.ExecuteQuery("SELECT * FROM TestTable WHERE Name = @Name",
            new() { ["Name"] = "Beta" });

        Assert.Single(table.Rows);
        Assert.Equal("Beta", table.Rows[0]["Name"]);
    }

    [Fact]
    public void LoadEntities_ShouldIncludeTestTable()
    {
        var entities = _db.LoadEntities("dbo").ToList();

        var testEntity = entities.FirstOrDefault(e => e?.Name == "TestTable");
        Assert.NotNull(testEntity);
        Assert.Contains("Name", testEntity.Attributes.Keys);
    }

    public void Dispose()
    {
        try
        {
            _db.ExecuteNonQuery("DROP TABLE IF EXISTS TestTable");
        }
        catch
        {
            // Ignore if already dropped or DB unreachable
        }

        try
        {
            // Force-stop LocalDB instance
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new()
                {
                    FileName = "sqllocaldb",
                    Arguments = "stop MSSQLLocalDB",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();
        }
        catch
        {
            // Ignore failure to stop
        }
    }
}
