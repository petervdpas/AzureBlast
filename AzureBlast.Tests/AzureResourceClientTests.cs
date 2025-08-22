using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager;
using AzureBlast.Interfaces;
using Moq;

namespace AzureBlast.Tests;

public sealed class AzureResourceClientTests
{
    private static IArmClientWrapper MakeRealArmClientWrapper()
    {
        var wrapper = new Mock<IArmClientWrapper>();

        // Real ArmClient is fine here: we won't list anything in these tests.
        var armClient = new ArmClient(new DefaultAzureCredential());

        wrapper.Setup(w => w.GetArmClient()).Returns(armClient);
        return wrapper.Object;
    }

    [Fact]
    public async Task SetSubscriptionContextAsync_Throws_OnNullOrWhitespace()
    {
        var client = new AzureResourceClient(MakeRealArmClientWrapper());

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SetSubscriptionContextAsync(null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SetSubscriptionContextAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SetSubscriptionContextAsync("   "));
    }

    [Fact]
    public async Task SetSubscriptionContextAsync_Sets_CurrentSubscription()
    {
        var client = new AzureResourceClient(MakeRealArmClientWrapper());

        // Any GUID-like string is fine; this does NOT call the network.
        var subId = Guid.NewGuid().ToString();

        await client.SetSubscriptionContextAsync(subId);

        Assert.NotNull(client.CurrentSubscription);
        // Location string should contain "/subscriptions/{id}"
        Assert.EndsWith($"/subscriptions/{subId}", client.CurrentSubscription.Id.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Methods_Throw_When_SubscriptionContext_Not_Set()
    {
        var client = new AzureResourceClient(MakeRealArmClientWrapper());

        // All methods that rely on EnsureSubscriptionContextIsSet should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResourceGroupsByTagAsync("env", "prod"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResourcesInResourceGroupAsync("rg1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResourcesByTypeAsync("Microsoft.Compute/virtualMachines"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CountResourcesByLocationAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResourcesByTagsAsync(new Dictionary<string, string> { ["env"] = "prod" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListResourceProvidersAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ResourceExistsAsync("rg1", "res1"));
    }

    [Fact]
    public void Class_DefaultCtor_Wires_DefaultCredentials()
    {
        // This ensures the default ctor is usable (no exceptions on construction).
        var client = new AzureResourceClient();
        Assert.Null(client.CurrentSubscription);
    }
}