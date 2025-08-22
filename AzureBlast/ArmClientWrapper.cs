using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
///     A wrapper for the Azure Resource Manager (ARM) client to facilitate interaction with Azure resources.
/// </summary>
public class ArmClientWrapper : IArmClientWrapper
{
    private readonly ArmClient _armClient;

    /// <summary>
    /// Initializes a new instance using a TokenCredential.
    /// </summary>
    public ArmClientWrapper(TokenCredential credential)
    {
        _armClient = new ArmClient(credential);
    }

    /// <summary>
    /// Internal constructor for test injection of mocked ArmClient.
    /// </summary>
    internal ArmClientWrapper(ArmClient armClient)
    {
        _armClient = armClient;
    }
    
    /// <inheritdoc />
    public ArmClient GetArmClient() => _armClient;

    /// <inheritdoc />
    public SubscriptionCollection GetSubscriptions() => _armClient.GetSubscriptions();
    
    /// <inheritdoc />
    public SubscriptionResource GetSubscriptionResource(string subscriptionId)
    {
        var resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
        return _armClient.GetSubscriptionResource(resourceIdentifier);
    }
}
