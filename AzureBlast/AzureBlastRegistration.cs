using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast
{
    /// <summary>
    /// Starts a fluent AzureBlast configuration on an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// Returns an <see cref="AzureBlastBuilder"/> so you can chain calls and finally <c>Build()</c>.
    /// </remarks>
    /// <example>
    /// services
    ///     .UseAzureBlast() // DefaultAzureCredential
    ///     .WithSql(cs)
    ///     .WithKeyVault(vaultUrl)
    ///     .Build();
    /// </example>
    public static class AzureBlastRegistration
    {
        /// <summary>
        /// Begins a fluent AzureBlast configuration chain bound to <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The target service collection.</param>
        /// <param name="credential">
        /// Optional <see cref="TokenCredential"/>; falls back to <see cref="DefaultAzureCredential"/>.
        /// </param>
        /// <returns>An <see cref="AzureBlastBuilder"/> to continue configuration.</returns>
        public static AzureBlastBuilder UseAzureBlast(
            this IServiceCollection services,
            TokenCredential? credential = null)
            => new(services, credential ?? new DefaultAzureCredential());
    }
}
