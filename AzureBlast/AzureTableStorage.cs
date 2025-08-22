using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
///     Provides methods for interacting with Azure Table Storage.
///     Implements <see cref="IAzureTableStorage" />.
/// </summary>
public class AzureTableStorage : IAzureTableStorage
{
    private string? _connectionString;
    private TableServiceClient? _serviceClient;
    private TableClient? _tableClient;
    private string? _tableName;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IAzureTableStorage" /> class.
    /// </summary>
    public AzureTableStorage()
    {
    }

    /// <summary>
    ///     Initializes the Table Storage client with a connection string and table name.
    ///     This must be called before performing any other operations.
    /// </summary>
    /// <param name="connectionString">The connection string for the Storage Account.</param>
    /// <param name="tableName">The name of the table to interact with.</param>
    public void Initialize(string connectionString, string? tableName)
    {
        _connectionString = connectionString;
        _serviceClient = new TableServiceClient(_connectionString);

        if (tableName == null) return;

        SetTable(tableName);
    }

    /// <summary>
    ///     Lists all tables in the connected Azure Table Storage account.
    /// </summary>
    /// <returns>A list of table names.</returns>
    public async Task<List<string>> ListTablesAsync()
    {
        EnsureServiceClientConfigured();

        var tableNames = new List<string>();
        await foreach (var tableItem in _serviceClient!.QueryAsync()) tableNames.Add(tableItem.Name);

        return tableNames;
    }

    /// <summary>
    ///     Sets the table to interact with after initialization.
    /// </summary>
    /// <param name="tableName">The name of the table to interact with.</param>
    public void SetTable(string tableName)
    {
        EnsureServiceClientConfigured();

        _tableName = tableName;
        _tableClient = new TableClient(_connectionString, _tableName);
    }

    /// <summary>
    ///     Adds or updates an entity in the table.
    /// </summary>
    /// <param name="entity">The entity to add or update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpsertEntityAsync<T>(T entity) where T : ITableEntity
    {
        EnsureConfigured();
        await _tableClient!.UpsertEntityAsync(entity);
    }

    /// <summary>
    ///     Deletes an entity from the table.
    /// </summary>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        EnsureConfigured();
        await _tableClient!.DeleteEntityAsync(partitionKey, rowKey);
    }

    /// <summary>
    ///     Retrieves an entity from the table by partition key and row key.
    /// </summary>
    /// <typeparam name="T">The type of the entity (must implement ITableEntity).</typeparam>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <returns>The entity if found, or null if not found.</returns>
    public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
    {
        EnsureConfigured();
        try
        {
            return await _tableClient!.GetEntityAsync<T>(partitionKey, rowKey);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null; // Entity not found
        }
    }

    /// <summary>
    ///     Queries entities in the table based on a filter.
    /// </summary>
    /// <typeparam name="T">The type of the entity (must implement ITableEntity).</typeparam>
    /// <param name="filter">The OData filter expression.</param>
    /// <returns>A list of entities that match the filter.</returns>
    public async Task<List<T>> QueryEntitiesAsync<T>(string filter) where T : class, ITableEntity, new()
    {
        EnsureConfigured();
        var entities = new List<T>();
        await foreach (var entity in _tableClient!.QueryAsync<T>(filter)) entities.Add(entity);
        return entities;
    }

    /// <summary>
    ///     Checks if the entities with the specified RowKeys exist in the Azure Table Storage.
    /// </summary>
    /// <typeparam name="T">The type of the entity (must implement <see cref="ITableEntity" />).</typeparam>
    /// <param name="rowKeys">The list of RowKeys to check for existence in the table.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, containing a list of RowKeys that exist in the table.
    /// </returns>
    /// <remarks>
    ///     This method queries the table for each RowKey provided in the list. If any entities with matching
    ///     RowKeys are found, they are added to the resulting list.
    /// </remarks>
    public async Task<List<string>> CheckEntitiesExistAsync<T>(List<string> rowKeys)
        where T : class, ITableEntity, new()
    {
        EnsureConfigured();

        var foundRowKeys = new List<string>();

        foreach (var rowKey in rowKeys)
        {
            var queryResults = await QueryEntitiesAsync<T>($"RowKey eq '{rowKey}'");

            if (queryResults.Count != 0) foundRowKeys.Add(rowKey);
        }

        return foundRowKeys;
    }

    /// <summary>
    ///     Ensures the table client is properly configured before performing any operations.
    /// </summary>
    private void EnsureConfigured()
    {
        if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_tableName))
            throw new InvalidOperationException(
                "AzureTableStorage is not configured. Call Initialize() before performing any operations.");
    }

    /// <summary>
    ///     Ensures the service client is properly configured before performing operations like listing tables.
    /// </summary>
    private void EnsureServiceClientConfigured()
    {
        if (string.IsNullOrEmpty(_connectionString) || _serviceClient == null)
            throw new InvalidOperationException(
                "AzureTableStorage is not configured. Call Initialize() before performing any operations.");
    }
}
