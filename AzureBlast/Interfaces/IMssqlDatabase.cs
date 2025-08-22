using System.Collections.Generic;
using AzureBlast.Models;

namespace AzureBlast.Interfaces;

/// <summary>
///     Represents an interface for MSSQL database operations.
///     Provides methods for executing queries, commands, and loading metadata.
/// </summary>
public interface IMssqlDatabase : IDatabase
{
    /// <summary>
    ///     Executes an INSERT SQL command and returns the number of rows affected.
    /// </summary>
    /// <param name="query">The SQL INSERT query to execute.</param>
    /// <param name="parameters">A dictionary of parameter names and values to include in the query.</param>
    /// <returns>The number of rows affected by the INSERT command.</returns>
    int ExecuteInsert(string query, Dictionary<string, object> parameters);

    /// <summary>
    ///     Executes an UPDATE SQL command and returns the number of rows affected.
    /// </summary>
    /// <param name="query">The SQL UPDATE query to execute.</param>
    /// <param name="parameters">A dictionary of parameter names and values to include in the query.</param>
    /// <returns>The number of rows affected by the UPDATE command.</returns>
    int ExecuteUpdate(string query, Dictionary<string, object> parameters);

    /// <summary>
    ///     Loads metadata about entities (tables and their columns) in a specified database schema.
    /// </summary>
    /// <param name="schema">The database schema to retrieve metadata for.</param>
    /// <param name="queryOverwrite">
    ///     An optional custom SQL query to use instead of the default query for loading entities.
    /// </param>
    /// <param name="cleaningToken">
    ///     An optional string to clean table names by removing a specified prefix or token.
    /// </param>
    /// <returns>
    ///     A collection of <see cref="Entity" /> objects representing the tables and their columns in the schema.
    /// </returns>
    IEnumerable<Entity?> LoadEntities(string schema, string? queryOverwrite = null, string? cleaningToken = null);

    /// <summary>
    ///     Loads metadata about relationships (foreign keys) between entities in a specified database schema.
    /// </summary>
    /// <param name="schema">The database schema to retrieve relationships for.</param>
    /// <param name="queryOverwrite">
    ///     An optional custom SQL query to use instead of the default query for loading relationships.
    /// </param>
    /// <param name="cleaningToken">
    ///     An optional string to clean entity names by removing a specified prefix or token.
    /// </param>
    /// <returns>
    ///     A collection of <see cref="Relationship" /> objects representing the foreign key relationships in the schema.
    /// </returns>
    IEnumerable<Relationship> LoadRelationships(string schema, string? queryOverwrite = null,
        string? cleaningToken = null);
}