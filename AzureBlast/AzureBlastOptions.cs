using Azure.Core;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Options used to configure which AzureBlast services are registered and how they are initialized.
/// </summary>
/// <remarks>
/// Only properties you populate are acted upon by the registration helpers such as
/// <see cref="ServiceCollectionExtensions.AddAzureBlast(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{AzureBlastOptions})" />.
/// Unset or empty values mean the corresponding component will not be registered.
/// </remarks>
/// <example>
/// <code>
/// services.AddAzureBlast(o =>
/// {
///     o.SqlConnectionString = "...";
///     o.KeyVaultUrl = "https://contoso.vault.azure.net/";
///     o.TableStorageConnectionString = "...";
///     o.TableName = "MyTable";
///     o.ServiceBusConnectionString = "...";
///     o.ServiceBusQueueName = "orders";
///     // Optional credential override (otherwise DefaultAzureCredential is used):
///     // o.Credential = new DefaultAzureCredential();
/// });
/// </code>
/// </example>
public class AzureBlastOptions
{
    /// <summary>
    /// SQL Server connection string used to register <see cref="IMssqlDatabase"/>.
    /// </summary>
    /// <remarks>
    /// If this is <see langword="null"/> or empty, the SQL component is not registered.
    /// </remarks>
    public string SqlConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified Azure Key Vault URL (for example, <c>https://contoso.vault.azure.net/</c>)
    /// used to register <see cref="IAzureKeyVault"/>.
    /// </summary>
    /// <remarks>
    /// If provided, the Key Vault client is created using <see cref="Credential"/> if set,
    /// otherwise a <c>DefaultAzureCredential</c> fallback is used.
    /// </remarks>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Azure Table Storage connection string used to register <see cref="IAzureTableStorage"/>.
    /// </summary>
    /// <remarks>
    /// If this is not set, the Table Storage component is not registered.
    /// </remarks>
    public string? TableStorageConnectionString { get; set; }

    /// <summary>
    /// Optional default table name used by <see cref="IAzureTableStorage"/> after initialization.
    /// </summary>
    /// <remarks>
    /// You can still switch tables later in code via <c>SetTable</c>.
    /// </remarks>
    public string? TableName { get; set; }

    /// <summary>
    /// Azure Service Bus connection string used to register <see cref="IAzureServiceBus"/>.
    /// </summary>
    /// <remarks>
    /// Both this and <see cref="ServiceBusQueueName"/> must be provided; if either is missing,
    /// the Service Bus component is not registered.
    /// </remarks>
    public string? ServiceBusConnectionString { get; set; }

    /// <summary>
    /// Azure Service Bus queue name associated with <see cref="ServiceBusConnectionString"/>.
    /// </summary>
    /// <remarks>
    /// Must be provided together with <see cref="ServiceBusConnectionString"/> to enable Service Bus registration.
    /// </remarks>
    public string? ServiceBusQueueName { get; set; }

    /// <summary>
    /// Optional Azure <see cref="TokenCredential"/> used by Azure SDK clients (Key Vault, ARM, etc.).
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, registration helpers default to <c>DefaultAzureCredential</c>.
    /// Provide an explicit credential when running outside Azure or when using custom auth flows.
    /// </remarks>
    public TokenCredential? Credential { get; set; }
}
