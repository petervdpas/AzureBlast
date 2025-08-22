using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureBlast.Interfaces;
using AzureBlast.Models;

namespace AzureBlast;

/// <summary>
///     A client for managing Azure resources using the Azure Resource Manager (ARM) SDK.
/// </summary>
public class AzureResourceClient : IAzureResourceClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ArmClient _armClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IAzureResourceClient" /> class with default credentials.
    /// </summary>
    public AzureResourceClient() : this(new ArmClientWrapper(new DefaultAzureCredential()))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureResourceClient" /> class with a custom
    ///     <see cref="IArmClientWrapper" />.
    /// </summary>
    /// <param name="armClientWrapper">The ARM client wrapper for managing Azure resources.</param>
    public AzureResourceClient(IArmClientWrapper armClientWrapper)
    {
        _armClient = armClientWrapper.GetArmClient();
    }

    /// <summary>
    ///     Gets or sets the current subscription context.
    /// </summary>
    public SubscriptionResource? CurrentSubscription { get; private set; }

    /// <summary>
    ///     Lists all available subscriptions for the authenticated Azure account.
    /// </summary>
    /// <returns>A collection of <see cref="SubscriptionResource" /> objects representing the subscriptions.</returns>
    public async Task<IEnumerable<SubscriptionResource>> ListSubscriptionsAsync()
    {
        var subscriptions = _armClient.GetSubscriptions();
        var subscriptionList = new List<SubscriptionResource>();

        await foreach (var subscription in subscriptions) subscriptionList.Add(subscription);

        return subscriptionList;
    }

    /// <summary>
    ///     Sets the current subscription context for further operations.
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to set as the current context.</param>
    public async Task SetSubscriptionContextAsync(string? subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentNullException(nameof(subscriptionId), "Subscription ID cannot be null or empty.");

        var resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
        CurrentSubscription = _armClient.GetSubscriptionResource(resourceIdentifier);

        if (CurrentSubscription == null)
            throw new InvalidOperationException("Failed to retrieve the subscription resource.");

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Gets resource groups with a specific tag and value.
    /// </summary>
    /// <param name="tagName">The name of the tag to filter by.</param>
    /// <param name="tagValue">The value of the tag to filter by.</param>
    /// <returns>A collection of <see cref="ResourceGroupResource" /> objects that match the tag.</returns>
    public async Task<IEnumerable<ResourceGroupResource>> GetResourceGroupsByTagAsync(string tagName, string tagValue)
    {
        EnsureSubscriptionContextIsSet();

        var resourceGroups = CurrentSubscription?.GetResourceGroups();
        var resourceGroupList = new List<ResourceGroupResource>();

        if (resourceGroups == null) return resourceGroupList;

        await foreach (var rg in resourceGroups)
            if (rg.Data.Tags.TryGetValue(tagName, out var value) && value == tagValue)
                resourceGroupList.Add(rg);

        return resourceGroupList;
    }

    /// <summary>
    ///     Gets all resources in a specified resource group.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <returns>A collection of <see cref="AzureResource" /> objects representing the resources in the group.</returns>
    public async Task<IEnumerable<AzureResource>> GetResourcesInResourceGroupAsync(string resourceGroupName)
    {
        EnsureSubscriptionContextIsSet();

        var resourceGroup = await CurrentSubscription?.GetResourceGroups().GetAsync(resourceGroupName)!;

        if (resourceGroup == null)
            throw new ArgumentException("Resource group not found.");

        var resources = resourceGroup.Value.GetGenericResources();

        return (from resource in resources
            let resourceIdParts = resource.Data.Id.ToString().Split('/')
            let resourceGroupNameExtracted = resourceIdParts.ElementAtOrDefault(4) ?? "Unknown"
            select new AzureResource
            {
                Name = resource.Data.Name,
                ResourceId = resource.Data.Id.ToString(),
                ResourceType = resource.Data.ResourceType.ToString(),
                Location = resource.Data.Location,
                ResourceGroupName = resourceGroupNameExtracted
            }).ToList();
    }

    /// <summary>
    ///     Gets all resources in a specified resource group and returns them as a JSON string.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <returns>A JSON string representing the resources in the group.</returns>
    public async Task<string> GetResourcesInResourceGroupAsJsonAsync(string resourceGroupName)
    {
        var resources = await GetResourcesInResourceGroupAsync(resourceGroupName);
        return JsonSerializer.Serialize(resources, JsonSerializerOptions);
    }

    /// <summary>
    ///     Saves all resources in a specified resource group to a JSON file.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <param name="filePath">The path to the file where the JSON will be saved.</param>
    public async Task SaveResourcesToJsonFileAsync(string resourceGroupName, string filePath)
    {
        var json = await GetResourcesInResourceGroupAsJsonAsync(resourceGroupName);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    ///     Gets all resources of a specific type across all resource groups.
    /// </summary>
    /// <param name="resourceType">The type of resources to filter by.</param>
    /// <returns>A collection of <see cref="AzureResource" /> objects of the specified type.</returns>
    public async Task<IEnumerable<AzureResource>> GetResourcesByTypeAsync(string resourceType)
    {
        EnsureSubscriptionContextIsSet();

        var resourceGroups = CurrentSubscription?.GetResourceGroups();
        var allResources = new List<AzureResource>();

        if (resourceGroups == null) return allResources;
        await foreach (var rg in resourceGroups)
        {
            var resources = rg.GetGenericResources();
            allResources.AddRange(from resource in resources
                where resource.Data.ResourceType == resourceType
                let resourceIdParts = resource.Data.Id.ToString().Split('/')
                let resourceGroupNameExtracted = resourceIdParts.ElementAtOrDefault(4) ?? "Unknown"
                select new AzureResource
                {
                    Name = resource.Data.Name,
                    ResourceId = resource.Data.Id.ToString(),
                    ResourceType = resource.Data.ResourceType.ToString(),
                    Location = resource.Data.Location,
                    ResourceGroupName = resourceGroupNameExtracted
                });
        }

        return allResources;
    }

    /// <summary>
    ///     Counts the number of resources by their location.
    /// </summary>
    /// <returns>A dictionary where the key is the location and the value is the count of resources.</returns>
    public async Task<Dictionary<string, int>> CountResourcesByLocationAsync()
    {
        if (CurrentSubscription == null)
            throw new InvalidOperationException("Subscription context is not set.");

        var resourceGroups = CurrentSubscription.GetResourceGroups();
        var locationCounts = new Dictionary<string, int>();

        await foreach (var rg in resourceGroups)
        {
            var resources = rg.GetGenericResources();
            foreach (var resource in resources)
            {
                string location = resource.Data.Location;
                if (!locationCounts.TryAdd(location, 1))
                    locationCounts[location]++;
            }
        }

        return locationCounts;
    }

    /// <summary>
    ///     Gets resources that match the specified tags.
    /// </summary>
    /// <param name="tags">A dictionary of tag names and values to filter by.</param>
    /// <returns>A collection of <see cref="AzureResource" /> objects that match the tags.</returns>
    public async Task<IEnumerable<AzureResource>> GetResourcesByTagsAsync(Dictionary<string, string> tags)
    {
        EnsureSubscriptionContextIsSet();

        var resourceGroups = CurrentSubscription?.GetResourceGroups();
        var matchingResources = new List<AzureResource>();

        if (resourceGroups == null) return matchingResources;
        await foreach (var rg in resourceGroups)
        {
            var resources = rg.GetGenericResources();
            foreach (var resource in resources)
            {
                if (!tags.All(tag =>
                        resource.Data.Tags.TryGetValue(tag.Key, out var value) && value == tag.Value)) continue;

                var resourceIdParts = resource.Data.Id.ToString().Split('/');
                var resourceGroupNameExtracted = resourceIdParts.ElementAtOrDefault(4) ?? "Unknown";

                matchingResources.Add(new AzureResource
                {
                    Name = resource.Data.Name,
                    ResourceId = resource.Data.Id.ToString(),
                    ResourceType = resource.Data.ResourceType.ToString(),
                    Location = resource.Data.Location,
                    ResourceGroupName = resourceGroupNameExtracted
                });
            }
        }

        return matchingResources;
    }

    /// <summary>
    ///     Lists all resource providers for the current subscription.
    /// </summary>
    /// <returns>A collection of strings representing the namespaces of the resource providers.</returns>
    public async Task<IEnumerable<string>> ListResourceProvidersAsync()
    {
        EnsureSubscriptionContextIsSet();

        var providers = CurrentSubscription?.GetResourceProviders();
        var providerList = new List<string>();

        if (providers == null) return providerList;
        await foreach (var provider in providers) providerList.Add(provider.Data.Namespace);

        return providerList;
    }

    /// <summary>
    ///     Checks if a resource exists in a specified resource group.
    /// </summary>
    /// <param name="resourceGroupName">The name of the resource group.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <returns><c>true</c> if the resource exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> ResourceExistsAsync(string resourceGroupName, string resourceName)
    {
        EnsureSubscriptionContextIsSet();

        var resourceGroup = await CurrentSubscription?.GetResourceGroups().GetAsync(resourceGroupName)!;

        if (resourceGroup == null)
            throw new ArgumentException("Resource group not found.");

        var resources = resourceGroup.Value.GetGenericResources();
        return resources.Any(resource => resource.Data.Name == resourceName);
    }

    /// <summary>
    ///     Ensures that the subscription context has been set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the subscription context is not set.</exception>
    private void EnsureSubscriptionContextIsSet()
    {
        if (CurrentSubscription == null) throw new InvalidOperationException("Subscription context is not set.");
    }
}