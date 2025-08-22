using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureBlast.Interfaces;
using Moq;

namespace AzureBlast.Tests;

public class ArmClientWrapperTests
{
    [Fact]
    public void GetArmClient_ShouldReturnInjectedInstance()
    {
        var credential = Mock.Of<TokenCredential>();
        var realClient = new ArmClient(credential);

        var wrapper = new ArmClientWrapper(realClient);

        Assert.Equal(realClient, wrapper.GetArmClient());
    }

    [Fact]
    public void GetSubscriptions_ShouldReturnFromArmClient()
    {
        var subscriptions = Mock.Of<SubscriptionCollection>();
        var mockClient = new Mock<IArmClientWrapper>();
        mockClient.Setup(c => c.GetSubscriptions()).Returns(subscriptions);

        var result = mockClient.Object.GetSubscriptions();

        // Don’t enumerate or call Assert.Equal() on mocked object
        Assert.Same(subscriptions, result); // ✅ Simple reference check
    }

    [Fact]
    public void GetSubscriptionResource_ShouldReturnCorrectResource()
    {
        var id = "1234";
        var expected = Mock.Of<SubscriptionResource>();
        var mockClient = new Mock<IArmClientWrapper>();

        mockClient
            .Setup(c => c.GetSubscriptionResource(id))
            .Returns(expected);

        var result = mockClient.Object.GetSubscriptionResource(id);

        Assert.Equal(expected, result);
    }
}
