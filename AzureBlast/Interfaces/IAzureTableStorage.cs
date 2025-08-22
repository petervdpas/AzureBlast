using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace AzureBlast.Interfaces;

/// <summary>
///     Provides methods for interacting with Azure Table Storage.
/// </summary>
public interface IAzureTableStorage
{
    /// <summary>
    ///     Initializes the Table Storage client with a connection string and table name.
    ///     This must be called before performing any other operations.
    /// </summary>
    /// <param name="connectionString">The connection string for the Storage Account.</param>
    /// <param name="tableName">The name of the table to interact with.</param>
    void Initialize(string connectionString, string? tableName);

    /// <summary>
    ///     Lists all tables in the connected Azure Table Storage account.
    /// </summary>
    /// <returns>A list of table names.</returns>
    Task<List<string>> ListTablesAsync();

    /// <summary>
    ///     Sets the table to interact with after initialization.
    /// </summary>
    /// <param name="tableName">The name of the table to interact with.</param>
    void SetTable(string tableName);

    /// <summary>
    ///     Adds or updates an entity in the table.
    /// </summary>
    /// <typeparam name="T">The type of the entity, which must implement <see cref="ITableEntity" />.</typeparam>
    /// <param name="entity">The entity to add or update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertEntityAsync<T>(T entity) where T : ITableEntity;

    /// <summary>
    ///     Deletes an entity from the table by its partition key and row key.
    /// </summary>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteEntityAsync(string partitionKey, string rowKey);

    /// <summary>
    ///     Retrieves an entity from the table using the specified partition key and row key.
    /// </summary>
    /// <typeparam name="T">The type of the entity, which must implement <see cref="ITableEntity" />.</typeparam>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <returns>A task representing the asynchronous operation, containing the entity if found, or null if not found.</returns>
    Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();

    /// <summary>
    ///     Queries entities in the table that match the specified OData filter.
    /// </summary>
    /// <typeparam name="T">The type of the entity, which must implement <see cref="ITableEntity" />.</typeparam>
    /// <param name="filter">The OData filter expression for querying entities.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of entities that match the filter.</returns>
    Task<List<T>> QueryEntitiesAsync<T>(string filter) where T : class, ITableEntity, new();

    /// <summary>
    ///     Checks if entities with the specified row keys exist in the table.
    /// </summary>
    /// <typeparam name="T">The type of the entity, which must implement <see cref="ITableEntity" />.</typeparam>
    /// <param name="rowKeys">A list of row keys to check for existence in the table.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of row keys that exist in the table.</returns>
    Task<List<string>> CheckEntitiesExistAsync<T>(List<string> rowKeys) where T : class, ITableEntity, new();
}
