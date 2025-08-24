using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AzureBlast.Utilities;

namespace AzureBlast.Tests;

public class PayloadFactoryTests
{
    [Fact]
    public void Single_WithValidKey_ReturnsDictionaryWithOneEntry()
    {
        var obj = PayloadFactory.Single("employeeId", "12345");

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"employeeId\":\"12345\"", json);
    }

    [Fact]
    public void Single_WithEmptyKey_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => PayloadFactory.Single("", 1));
        Assert.Equal("key", ex.ParamName, ignoreCase: true);
    }

    [Fact]
    public void Many_IgnoresNullOrWhitespaceKeys_AndKeepsOthers()
    {
        var obj = PayloadFactory.Many(
            ("employeeId", "E-1"),
            ("", "skip"),
            ("  ", 42),
            (null!, "also skip"),
            ("active", true)
        );

        var json = JsonSerializer.Serialize(obj);
        Assert.Contains("\"employeeId\":\"E-1\"", json);
        Assert.Contains("\"active\":true", json);
        Assert.DoesNotContain("skip", json);
        Assert.DoesNotContain("\"  \"", json);
    }

    [Fact]
    public void Many_LastDuplicateKeyWins()
    {
        var obj = PayloadFactory.Many(
            ("status", "pending"),
            ("status", "approved")
        );

        var json = JsonSerializer.Serialize(obj);
        Assert.Contains("\"status\":\"approved\"", json);
        Assert.DoesNotContain("\"status\":\"pending\"", json);
    }

    [Fact]
    public void Event_WithExplicitTimestamp_IncludesIso8601()
    {
        var ts = new DateTimeOffset(2025, 08, 24, 12, 34, 56, TimeSpan.Zero);
        var extra = new Dictionary<string, object?>
        {
            ["employeeId"] = "E-99",
            ["note"] = null
        };

        var obj = PayloadFactory.Event("hire", ts, extra);

        var json = JsonSerializer.Serialize(
            obj,
            new JsonSerializerOptions {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("hire", root.GetProperty("id").GetString());
        Assert.Equal(ts.ToString("o"), root.GetProperty("timestamp").GetString());  // <- key change
        Assert.Equal("E-99", root.GetProperty("employeeId").GetString());
        Assert.True(root.TryGetProperty("note", out var noteProp));
        Assert.Equal(JsonValueKind.Null, noteProp.ValueKind);
    }

    [Fact]
    public void Event_WithNoExtraData_ContainsIdAndTimestampOnly()
    {
        var ts = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero);
        var obj = PayloadFactory.Event("boot", ts);

        var json = JsonSerializer.Serialize(obj);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("boot", root.GetProperty("id").GetString());
        Assert.Equal(ts.ToString("o"), root.GetProperty("timestamp").GetString());  // <- key change

        // Optionally assert there are only two properties:
        Assert.Equal(2, root.EnumerateObject().Count());
    }

    [Fact]
    public void Command_WithTypeAndData_MergesFields()
    {
        var data = new Dictionary<string, object?>
        {
            ["employeeId"] = "E-3",
            ["priority"] = 5
        };

        var obj = PayloadFactory.Command("ProcessEmployee", data);
        var json = JsonSerializer.Serialize(obj);

        Assert.Contains("\"commandType\":\"ProcessEmployee\"", json);
        Assert.Contains("\"employeeId\":\"E-3\"", json);
        Assert.Contains("\"priority\":5", json);
    }

    [Fact]
    public void Command_WithNullData_HasOnlyCommandType()
    {
        var obj = PayloadFactory.Command("Ping");
        var json = JsonSerializer.Serialize(obj);

        Assert.Contains("\"commandType\":\"Ping\"", json);
        // should not throw, and other fields aren’t mandated
    }
}