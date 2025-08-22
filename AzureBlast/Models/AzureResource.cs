namespace AzureBlast.Models;

/// <summary>
///     Represents a generic Azure resource with its essential properties.
/// </summary>
public class AzureResource
{
    /// <summary>
    ///     Gets or sets the name of the resource.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the unique identifier (ID) of the resource.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the type of the resource (e.g., "Microsoft.Compute/virtualMachines").
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the geographic location of the resource.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the resource group containing this resource.
    /// </summary>
    public string ResourceGroupName { get; set; } = string.Empty;
}