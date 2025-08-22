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
    private readonly Func<string, TableServiceClient> _serviceFactory;
    private readonly Func<string, string, TableClient> _tableFactory;

    private string? _connectionString;
    private TableServiceClient? _serviceClient;
    private TableClient? _tableClient;
    private string? _tableName;

    /// <summary>
    ///     Initializes a new instance with default factories (real SDK clients).
    /// </summary>
    public AzureTableStorage()
        : this(cs => new TableServiceClient(cs), (cs, tn) => new TableClient(cs, tn))
    {
    }

    /// <summary>
    ///     Initializes a new instance with custom factories (for testing).
    /// </summary>
    internal AzureTableStorage(
        Func<string, TableServiceClient> serviceFactory,
        Func<string, string, TableClient> tableFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _tableFactory = tableFactory ?? throw new ArgumentNullException(nameof(tableFactory));
    }

    /// <inheritdoc />
    public void Initialize(string connectionString, string? tableName)
    {
        _connectionString = connectionString;
        _serviceClient = _serviceFactory(_connectionString);

        if (tableName != null)
        {
            SetTable(tableName);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> ListTablesAsync()
    {
        EnsureServiceClientConfigured();

        var tableNames = new List<string>();
        await foreach (var tableItem in _serviceClient!.QueryAsync())
            tableNames.Add(tableItem.Name);

        return tableNames;
    }

    /// <inheritdoc />
    public void SetTable(string tableName)
    {
        EnsureServiceClientConfigured();

        _tableName = tableName;
        _tableClient = _tableFactory(_connectionString!, _tableName);
    }

    /// <inheritdoc />
    public async Task UpsertEntityAsync<T>(T entity) where T : ITableEntity
    {
        EnsureConfigured();
        await _tableClient!.UpsertEntityAsync(entity);
    }

    /// <inheritdoc />
    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        EnsureConfigured();
        await _tableClient!.DeleteEntityAsync(partitionKey, rowKey);
    }

    /// <inheritdoc />
    public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey)
        where T : class, ITableEntity, new()
    {
        EnsureConfigured();
        try
        {
            // NOTE: GetEntityAsync returns Response<T>; return its Value.
            var resp = await _tableClient!.GetEntityAsync<T>(partitionKey, rowKey);
            return resp.Value;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<T>> QueryEntitiesAsync<T>(string filter)
        where T : class, ITableEntity, new()
    {
        EnsureConfigured();
        var entities = new List<T>();
        await foreach (var entity in _tableClient!.QueryAsync<T>(filter))
            entities.Add(entity);
        return entities;
    }

    /// <inheritdoc />
    public async Task<List<string>> CheckEntitiesExistAsync<T>(List<string> rowKeys)
        where T : class, ITableEntity, new()
    {
        EnsureConfigured();

        var found = new List<string>();
        foreach (var rowKey in rowKeys)
        {
            var results = await QueryEntitiesAsync<T>($"RowKey eq '{rowKey}'");
            if (results.Count > 0) found.Add(rowKey);
        }

        return found;
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
