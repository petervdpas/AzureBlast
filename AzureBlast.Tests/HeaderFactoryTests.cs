using System;
using System.Collections.Generic;
using AzureBlast.Utilities;

namespace AzureBlast.Tests;

public class HeaderFactoryTests
{
    [Fact]
    public void Empty_ReturnsDictionaryWithZeroCount()
    {
        var headers = HeaderFactory.Empty();
        Assert.NotNull(headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void Create_FromNull_ReturnsEmpty()
    {
        (string Key, object Value)[]? input = null;
        var headers = HeaderFactory.Create(input);
        Assert.NotNull(headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void Create_IgnoresNullOrWhitespaceKeys_AndKeepsOthers()
    {
        var headers = HeaderFactory.Create(
            ("tenant", "fontys"),
            ("", "ignore"),
            ("  ", 123),
            (null!, "ignore"),
            ("region", "euw")
        );

        Assert.Equal(2, headers.Count);
        Assert.Equal("fontys", headers["tenant"]);
        Assert.Equal("euw", headers["region"]);
        Assert.False(headers.ContainsKey(""));
        Assert.False(headers.ContainsKey("  "));
    }

    [Fact]
    public void Create_LastDuplicateValueWins_ForSameKey()
    {
        var headers = HeaderFactory.Create(
            ("key", "v1"),
            ("key", "v2"),
            ("key", "v3")
        );

        Assert.Single(headers);
        Assert.Equal("v3", headers["key"]);
    }

    [Fact]
    public void Create_UsesOrdinalComparer_CaseSensitiveKeysRemainDistinct()
    {
        var headers = HeaderFactory.Create(
            ("Key", "upper"),
            ("key", "lower")
        );

        Assert.Equal(2, headers.Count);
        Assert.Equal("upper", headers["Key"]);
        Assert.Equal("lower", headers["key"]);
    }

    [Fact]
    public void From_Null_ReturnsEmpty()
    {
        IDictionary<string, object>? src = null;
        var copy = HeaderFactory.From(src);

        Assert.NotNull(copy);
        Assert.Empty(copy);
    }

    [Fact]
    public void From_ReturnsCopy_NotAffectedBySourceMutation()
    {
        var src = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["k1"] = "v1",
            ["k2"] = "v2"
        };

        var copy = HeaderFactory.From(src);

        // mutate source after copy
        src["k1"] = "changed";
        src["k3"] = "v3";

        Assert.Equal(2, copy.Count);
        Assert.Equal("v1", copy["k1"]);
        Assert.Equal("v2", copy["k2"]);
        Assert.False(copy.ContainsKey("k3"));
    }

    [Fact]
    public void Merge_BothNullOrEmpty_ReturnsEmpty()
    {
        var merged1 = HeaderFactory.Merge(null, null);
        var merged2 = HeaderFactory.Merge(HeaderFactory.Empty(), HeaderFactory.Empty());

        Assert.Empty(merged1);
        Assert.Empty(merged2);
    }

    [Fact]
    public void Merge_BaseOnly_ReturnsBaseCopy()
    {
        var @base = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var merged = HeaderFactory.Merge(@base, null);

        // mutate base after merge
        @base["a"] = 999;
        @base["c"] = 3;

        Assert.Equal(2, merged.Count);
        Assert.Equal(1, merged["a"]);
        Assert.Equal(2, merged["b"]);
        Assert.False(merged.ContainsKey("c"));
    }

    [Fact]
    public void Merge_OverridesOnly_ReturnsOverridesCopy()
    {
        var overrides = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["x"] = "X",
            ["y"] = "Y"
        };

        var merged = HeaderFactory.Merge(null, overrides);

        // mutate overrides after merge
        overrides["x"] = "changed";

        Assert.Equal(2, merged.Count);
        Assert.Equal("X", merged["x"]);
        Assert.Equal("Y", merged["y"]);
    }

    [Fact]
    public void Merge_OverridesTakePrecedence_OnKeyOverlap()
    {
        var @base = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["k"] = "base",
            ["keep"] = 1
        };
        var overrides = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["k"] = "override",
            ["new"] = 2
        };

        var merged = HeaderFactory.Merge(@base, overrides);

        Assert.Equal(3, merged.Count);
        Assert.Equal("override", merged["k"]); // override wins
        Assert.Equal(1, merged["keep"]);
        Assert.Equal(2, merged["new"]);
    }

    [Fact]
    public void Merge_UsesOrdinalComparer_CaseSensitiveKeysRemainDistinct()
    {
        var @base = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Key"] = "upper"
        };
        var overrides = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["key"] = "lower"
        };

        var merged = HeaderFactory.Merge(@base, overrides);

        Assert.Equal(2, merged.Count);
        Assert.Equal("upper", merged["Key"]);
        Assert.Equal("lower", merged["key"]);
    }
}