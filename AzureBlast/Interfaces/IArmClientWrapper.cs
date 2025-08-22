using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace AzureBlast.Interfaces;

/// <summary>
///     Defines a wrapper interface for the Azure Resource Manager (ARM) client to interact with Azure resources.
/// </summary>
public interface IArmClientWrapper
{
    /// <summary>
    ///     Retrieves the underlying ARM client instance.
    /// </summary>
    /// <returns>The <see cref="ArmClient" /> instance used for managing Azure resources.</returns>
    ArmClient GetArmClient();

    /// <summary>
    ///     Retrieves a collection of subscriptions available to the authenticated user.
    /// </summary>
    /// <returns>A <see cref="SubscriptionCollection" /> containing the subscriptions.</returns>
    SubscriptionCollection GetSubscriptions();

    /// <summary>
    ///     Retrieves a specific subscription resource by its ID.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to retrieve.</param>
    /// <returns>
    ///     A <see cref="SubscriptionResource" /> representing the specified subscription.
    /// </returns>
    SubscriptionResource GetSubscriptionResource(string subscriptionId);
}