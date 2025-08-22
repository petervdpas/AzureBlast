using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.ResourceManager.Resources;
using AzureBlast.Models;

namespace AzureBlast.Interfaces;

/// <summary>
///     Provides an interface for managing Azure resources using the Azure Resource Manager (ARM) SDK.
/// </summary>
public interface IAzureResourceClient
{
    /// <summary>
    ///     Lists all subscriptions available for the authenticated Azure account.
    /// </summary>
    /// <returns>A collection of <see cref="SubscriptionResource" /> representing the subscriptions.</returns>
    Task<IEnumerable<SubscriptionResource>> ListSubscriptionsAsync();

    /// <summary>
    ///     Sets the current subscription context for resource management operations.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID to set as the current context.</param>
    Task SetSubscriptionContextAsync(string? subscriptionId);

    /// <summary>
    ///     Retrieves resource groups with a specific tag name and value.
    /// </summary>
    /// <param name="tagName">The name of the tag to filter by.</param>
    /// <param name="tagValue">The value of the tag to filter by.</param>
    /// <returns>A collection of <see cref="ResourceGroupResource" /> matching the specified tag.</returns>
    Task<IEnumerable<ResourceGroupResource>> GetResourceGroupsByTagAsync(string tagName, string tagValue);

    /// <summary>
    ///     Gets all resources within a specified resource group.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to query.</param>
    /// <returns>A collection of <see cref="AzureResource" /> representing the resources in the group.</returns>
    Task<IEnumerable<AzureResource>> GetResourcesInResourceGroupAsync(string resourceGroupName);

    /// <summary>
    ///     Gets all resources within a specified resource group and returns them as a JSON string.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to query.</param>
    /// <returns>A JSON-formatted string representing the resources in the group.</returns>
    Task<string> GetResourcesInResourceGroupAsJsonAsync(string resourceGroupName);

    /// <summary>
    ///     Saves all resources within a specified resource group to a JSON file.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to query.</param>
    /// <param name="filePath">The file path to save the JSON data.</param>
    Task SaveResourcesToJsonFileAsync(string resourceGroupName, string filePath);

    /// <summary>
    ///     Retrieves all resources of a specified type across all resource groups in the current subscription.
    /// </summary>
    /// <param name="resourceType">The type of resources to filter by.</param>
    /// <returns>A collection of <see cref="AzureResource" /> representing the matching resources.</returns>
    Task<IEnumerable<AzureResource>> GetResourcesByTypeAsync(string resourceType);

    /// <summary>
    ///     Counts resources by their location within the current subscription.
    /// </summary>
    /// <returns>A dictionary where keys are location names and values are resource counts.</returns>
    Task<Dictionary<string, int>> CountResourcesByLocationAsync();

    /// <summary>
    ///     Retrieves resources that match a set of specified tags.
    /// </summary>
    /// <param name="tags">A dictionary of tag names and values to filter by.</param>
    /// <returns>A collection of <see cref="AzureResource" /> representing the matching resources.</returns>
    Task<IEnumerable<AzureResource>> GetResourcesByTagsAsync(Dictionary<string, string> tags);

    /// <summary>
    ///     Lists all resource providers available in the current subscription.
    /// </summary>
    /// <returns>A collection of strings representing the namespaces of the resource providers.</returns>
    Task<IEnumerable<string>> ListResourceProvidersAsync();

    /// <summary>
    ///     Checks if a specific resource exists within a resource group.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group to search.</param>
    /// <param name="resourceName">The name of the resource to check for.</param>
    /// <returns><c>true</c> if the resource exists; otherwise, <c>false</c>.</returns>
    Task<bool> ResourceExistsAsync(string resourceGroupName, string resourceName);
}