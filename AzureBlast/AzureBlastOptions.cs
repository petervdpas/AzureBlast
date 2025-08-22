using Azure.Core;

namespace AzureBlast;

/// <summary>
///     Options for configuring AzureBlast services.
/// </summary>
public class AzureBlastOptions
{
    public string SqlConnectionString { get; set; } = string.Empty;
    public string? KeyVaultUrl { get; set; }
    public string? TableStorageConnectionString { get; set; }
    public string? TableName { get; set; }
    public string? ServiceBusConnectionString { get; set; }
    public string? ServiceBusQueueName { get; set; }
    public TokenCredential? Credential { get; set; }
}