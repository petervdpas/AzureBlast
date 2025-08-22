using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AzureBlast.Interfaces;
using AzureBlast.Models;
using Microsoft.Data.SqlClient;
using Serilog;

namespace AzureBlast;

/// <summary>
///     A helper class that provides MSSQL-specific database methods using Azure AD Interactive Authentication.
///     Implements <see cref="IMssqlDatabase" />.
/// </summary>
public class MssqlDatabase : IMssqlDatabase
{
    private SqlConnection? _connection;
    private string? _connectionString;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IMssqlDatabase" /> class.
    /// </summary>
    public MssqlDatabase()
    {
    }

    /// <summary>
    ///     Sets up the connection string for the MSSQL database.
    /// </summary>
    /// <param name="connectionString">The connection string to initialize the MSSQL database connection.</param>
    public void Setup(string connectionString)
    {
        if (!IsValidSqlConnectionString(connectionString))
            throw new ArgumentException("The provided connection string is not valid.");

        _connectionString = connectionString;
    }

    /// <summary>
    ///     Opens a connection to the SQL Server.
    /// </summary>
    public void OpenConnection()
    {
        if (_connection != null && _connection.State != ConnectionState.Closed) return;

        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException(
                "Connection string is not set. Call Setup with a valid connection string.");

        try
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Connection Error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected Error while opening connection: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Closes the database connection.
    /// </summary>
    public void CloseConnection()
    {
        if (_connection?.State == ConnectionState.Open) _connection.Close();
    }

    /// <summary>
    ///     Retrieves the active database connection for the MSSQL database.
    /// </summary>
    /// <returns>The active <see cref="IDbConnection" /> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the connection is not initialized or is not open.
    /// </exception>
    /// <remarks>
    ///     This method provides access to the underlying database connection used by the MSSQL database.
    ///     Ensure the connection is properly opened by calling <see cref="OpenConnection" /> before using this method.
    /// </remarks>
    public IDbConnection GetConnection()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
            throw new InvalidOperationException("The database connection is not open.");
        return _connection;
    }

    /// <summary>
    ///     Executes a parameterized non-query SQL statement, such as INSERT, UPDATE, or DELETE.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and values to add to the query.</param>
    /// <returns>The number of rows affected by the query.</returns>
    public int ExecuteNonQuery(string query, Dictionary<string, object>? parameters)
    {
        OpenConnection();
        try
        {
            using var command = new SqlCommand(query, _connection);

            if (parameters == null) return command.ExecuteNonQuery();

            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);

            return command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Error in ExecuteNonQuery with parameters: {ex.Message}");
            throw;
        }
        finally
        {
            CloseConnection();
        }
    }

    /// <summary>
    ///     Executes a parameterized SQL query that returns a single value (e.g., COUNT, SUM).
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and values to add to the query.</param>
    /// <returns>The value returned by the query, or null if no value is found.</returns>
    public object? ExecuteScalar(string query, Dictionary<string, object>? parameters)
    {
        OpenConnection();
        try
        {
            using var command = new SqlCommand(query, _connection);

            if (parameters == null) return command.ExecuteScalar();

            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);

            return command.ExecuteScalar();
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Error in ExecuteScalar with parameters: {ex.Message}");
            throw;
        }
        finally
        {
            CloseConnection();
        }
    }


    /// <summary>
    ///     Executes a parameterized SQL query and returns a <see cref="DataTable" /> result.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and values to add to the query.</param>
    /// <returns>A <see cref="DataTable" /> containing the result set of the query.</returns>
    public DataTable ExecuteQuery(string query, Dictionary<string, object>? parameters)
    {
        OpenConnection();
        try
        {
            var dataTable = new DataTable();
            using var command = new SqlCommand(query, _connection);

            if (parameters != null)
                foreach (var parameter in parameters)
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);

            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            return dataTable;
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Error in ExecuteQuery with parameters: {ex.Message}");
            throw;
        }
        finally
        {
            CloseConnection();
        }
    }

    /// <summary>
    ///     Executes an INSERT SQL query with parameters.
    /// </summary>
    /// <param name="query">The SQL INSERT query to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and their values.</param>
    /// <returns>The number of rows affected by the INSERT operation.</returns>
    public int ExecuteInsert(string query, Dictionary<string, object> parameters)
    {
        OpenConnection();
        try
        {
            using var command = new SqlCommand(query, _connection);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            return command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Error in ExecuteInsert: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected Error in ExecuteInsert: {ex.Message}");
            throw;
        }
        finally
        {
            CloseConnection();
        }
    }

    /// <summary>
    ///     Executes an UPDATE SQL query with parameters.
    /// </summary>
    /// <param name="query">The SQL UPDATE query to execute.</param>
    /// <param name="parameters">Dictionary of parameter names and their values.</param>
    /// <returns>The number of rows affected by the UPDATE operation.</returns>
    public int ExecuteUpdate(string query, Dictionary<string, object> parameters)
    {
        OpenConnection();
        try
        {
            using var command = new SqlCommand(query, _connection);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            return command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            Log.Error($"SQL Error in ExecuteUpdate: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected Error in ExecuteUpdate: {ex.Message}");
            throw;
        }
        finally
        {
            CloseConnection();
        }
    }

    /// <summary>
    ///     Loads a collection of entities from the database schema.
    /// </summary>
    /// <param name="schema">
    ///     The name of the schema from which to load entities.
    ///     This is typically the database schema that organizes tables.
    /// </param>
    /// <param name="queryOverwrite">
    ///     Optional parameter that allows overriding the default SQL query.
    ///     This can be used when the target RDBMS does not
    ///     follow standard SQL conventions or when a custom query is needed.
    /// </param>
    /// <param name="cleaningToken">
    ///     An optional string used to clean table names, such as removing schema prefixes.
    ///     If provided, this token will be removed from the beginning of table names.
    /// </param>
    /// <returns>
    ///     A collection of <see cref="Entity" /> objects
    ///     representing the tables and their columns in the specified schema.
    ///     Each entity contains its attributes (fields) such as column names,
    ///     data types, and whether the column is nullable.
    /// </returns>
    public IEnumerable<Entity?> LoadEntities(string schema, string? queryOverwrite = null, string? cleaningToken = null)
    {
        const string defaultQuery = """
                                        SELECT TABLE_NAME AS TableName, 
                                               COLUMN_NAME AS ColumnName, 
                                               DATA_TYPE AS DataType, 
                                               CHARACTER_MAXIMUM_LENGTH AS CharMaxLength, 
                                               IS_NULLABLE AS IsNullable
                                        FROM INFORMATION_SCHEMA.COLUMNS
                                        WHERE TABLE_SCHEMA = @Schema
                                        ORDER BY TABLE_NAME, ORDINAL_POSITION
                                    """;

        var query = BuildQuery(defaultQuery, queryOverwrite);
        var dataTable = ExecuteQueryWithSchema(query, schema);

        var entities = new List<Entity?>();
        Entity? currentEntity = null;

        foreach (DataRow row in dataTable.Rows)
        {
            var tableName = CleanEntityName(row["TableName"].ToString() ?? string.Empty, cleaningToken);

            if (currentEntity == null || currentEntity.Name != tableName)
            {
                if (currentEntity != null)
                    entities.Add(currentEntity);

                currentEntity = new Entity(tableName, new Dictionary<string, object>());
            }

            var fieldName = row["ColumnName"].ToString() ?? string.Empty;
            var fieldType = row["DataType"].ToString() ?? string.Empty;
            var isNullable = row["IsNullable"].ToString()?.Equals("YES", StringComparison.InvariantCultureIgnoreCase) ??
                             false;

            if (row["CharMaxLength"] != DBNull.Value)
            {
                var length = Convert.ToInt32(row["CharMaxLength"]);
                fieldType += $"({length})";
            }

            var fieldAttributes = new Dictionary<string, object>
            {
                { "Type", fieldType },
                { "IsNullable", isNullable }
            };

            currentEntity.Attributes.Add(fieldName, fieldAttributes);
        }

        if (currentEntity != null)
            entities.Add(currentEntity);

        return entities;
    }

    /// <summary>
    ///     Loads relationships (foreign key constraints) between entities in the database schema.
    /// </summary>
    /// <param name="schema">
    ///     The name of the schema from which to load relationships.
    ///     This is typically the database schema that organizes
    ///     tables.
    /// </param>
    /// <param name="queryOverwrite">
    ///     Optional parameter to override the default SQL query for fetching relationships.
    ///     Use this when the target RDBMS
    ///     does not follow standard SQL conventions or a custom query is needed.
    /// </param>
    /// <param name="cleaningToken">
    ///     An optional string used to clean entity names, such as removing schema prefixes.
    ///     If provided, this token will be removed from the beginning of entity names.
    /// </param>
    /// <returns>
    ///     A collection of <see cref="Relationship" /> objects
    ///     representing the foreign key relationships between tables in the schema.
    ///     Each relationship includes the source and target tables, as well as the key column used in the relationship.
    /// </returns>
    public IEnumerable<Relationship> LoadRelationships(string schema, string? queryOverwrite = null,
        string? cleaningToken = null)
    {
        const string defaultQuery = """
                                        
                                            SELECT fk.TABLE_NAME AS fkTableName, 
                                                   fk.COLUMN_NAME AS fkColumnNaam, 
                                                   pk.TABLE_NAME AS pkTableName, 
                                                   pk.COLUMN_NAME AS pkColumnNaam, 
                                                   fk.CONSTRAINT_NAME AS Name
                                            FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
                                            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS fk
                                              ON rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
                                            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS pk
                                              ON rc.UNIQUE_CONSTRAINT_NAME = pk.CONSTRAINT_NAME
                                            WHERE fk.TABLE_SCHEMA = @Schema
                                            ORDER BY fkTableName, fkColumnNaam
                                    """;

        var query = BuildQuery(defaultQuery, queryOverwrite);
        var dataTable = ExecuteQueryWithSchema(query, schema);

        return (from DataRow row in dataTable.Rows
            let fromEntity = row["fkTableName"].ToString() ?? string.Empty
            let toEntity = row["pkTableName"].ToString() ?? string.Empty
            select new Relationship
            {
                FromEntity = CleanEntityName(fromEntity, cleaningToken),
                ToEntity = CleanEntityName(toEntity, cleaningToken),
                Key = row["fkColumnNaam"].ToString() ?? string.Empty
            }).ToList();
    }

    /// <summary>
    ///     Cleans the entity name by removing the specified cleaning token if present.
    /// </summary>
    /// <param name="entityName">The original entity name.</param>
    /// <param name="cleaningToken">The token to remove from the entity name.</param>
    /// <returns>The cleaned entity name.</returns>
    private static string CleanEntityName(string entityName, string? cleaningToken)
    {
        if (!string.IsNullOrEmpty(cleaningToken) &&
            entityName.StartsWith(cleaningToken, StringComparison.OrdinalIgnoreCase))
            return entityName.Substring(cleaningToken.Length);
        return entityName;
    }

    /// <summary>
    ///     Basic validation to check for essential parts of an SQL Server connection string
    /// </summary>
    private static bool IsValidSqlConnectionString(string connectionString)
    {
        return connectionString.Contains("Server=") && connectionString.Contains("Database=");
    }

    /// <summary>
    ///     Builds the SQL query by using a default query or an optional query override.
    /// </summary>
    /// <param name="defaultQuery">
    ///     The default SQL query to use if no override is provided.
    /// </param>
    /// <param name="queryOverwrite">
    ///     Optional query that overrides the default SQL query. If <c>null</c> or empty, the <paramref name="defaultQuery" />
    ///     is used.
    /// </param>
    /// <returns>
    ///     The SQL query string that will be executed, either the default or the overwritten query.
    /// </returns>
    private string BuildQuery(string defaultQuery, string? queryOverwrite)
    {
        return string.IsNullOrEmpty(queryOverwrite) ? defaultQuery : queryOverwrite;
    }

    /// <summary>
    ///     Executes the provided SQL query with the schema parameter passed to it.
    /// </summary>
    /// <param name="query">
    ///     The SQL query to execute, with a placeholder for the schema.
    /// </param>
    /// <param name="schema">
    ///     The database schema to be used in the query. This parameter is passed as a query parameter to prevent SQL
    ///     injection.
    /// </param>
    /// <returns>
    ///     A <see cref="DataTable" /> containing the results of the SQL query.
    /// </returns>
    private DataTable ExecuteQueryWithSchema(string query, string schema)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@Schema", schema }
        };

        return ExecuteQuery(query, parameters);
    }
}