# AzureBlast

**AzureBlast** is a lightweight, injectable .NET library that simplifies working with **Azure services**—including **SQL Server**, **Service Bus**, **Key Vault**, **Table Storage**, and **ARM (Resource Manager)**—from applications, scripts, and tools. It’s designed to be DI-friendly and easy to unit test.

## ✨ Features

* **MSSQL** – simple API for parameterized queries and metadata loading.
* **Azure Service Bus** – send single/batch/scheduled messages; receive & complete.
* **Azure Key Vault** – initialize a vault and get/set/delete/purge secrets.
* **Azure Table Storage** – list tables, set a table, upsert/query/delete entities.
* **Azure Resource Manager (ARM)** – list subscriptions, set subscription context, query resources.
* **Two DI styles** – options-based `AddAzureBlast(...)` **or** a fluent builder via `UseAzureBlast(...)`.
* **Script-friendly** – `AzureBlastFactory` for LINQPad/PowerShell/console scenarios.
* **Testable** – interfaces and adapters to mock sealed SDK types.

## 📦 Installation

```bash
dotnet add package AzureBlast
```

> Targets .NET 8+. Authentication defaults to `DefaultAzureCredential` unless you supply a `TokenCredential`.

## 🔐 Authentication

By default, AzureBlast uses `DefaultAzureCredential`. You can pass a custom `TokenCredential` if you need a specific auth flow (client secret, managed identity from a particular resource, etc.).

```csharp
using Azure.Identity;
var cred = new ClientSecretCredential(tenantId, clientId, clientSecret);
```

---

## 🧰 Choose your setup

### 1) Options-based DI registration (one-shot)

```csharp
using AzureBlast;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity; // only if you override Credential

var services = new ServiceCollection();

services.AddAzureBlast(o =>
{
    o.SqlConnectionString          = "Server=.;Database=App;Trusted_Connection=True;";
    o.KeyVaultUrl                  = "https://contoso.vault.azure.net/";
    o.TableStorageConnectionString = "UseDevelopmentStorage=true;";
    o.TableName                    = "MyTable";
    o.ServiceBusConnectionString   = "<sb-connection-string>";
    o.ServiceBusQueueName          = "orders";

    // Optional credential override (otherwise DefaultAzureCredential is used):
    // o.Credential = new DefaultAzureCredential();
});

var sp = services.BuildServiceProvider();
```

> This path uses `TryAdd*` so **your own prior registrations aren’t overwritten**.

### 2) Fluent builder (incremental)

```csharp
using AzureBlast;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;

var services = new ServiceCollection();

// DefaultAzureCredential:
services
    .UseAzureBlast()
    .WithSql("Server=.;Database=App;Trusted_Connection=True;")
    .WithKeyVault("https://contoso.vault.azure.net/")
    .WithTableStorage("UseDevelopmentStorage=true;", "MyTable")
    .WithServiceBus("<sb-connection-string>", "orders")
    .Build();

// Or supply a custom TokenCredential:
services
    .UseAzureBlast(new DefaultAzureCredential())
    .WithKeyVault("https://contoso.vault.azure.net/")
    .Build();

var sp = services.BuildServiceProvider();
```

### 3) No-DI / Script usage (LINQPad, PowerShell, console)

```csharp
using AzureBlast;
using Azure.Identity;

// Build a provider with options:
var sp = AzureBlastFactory.CreateServiceProvider(o =>
{
    o.TableStorageConnectionString = "UseDevelopmentStorage=true;";
    o.TableName = "MyTable";
});

// Or construct individual services directly:
var kv = AzureBlastFactory.CreateKeyVault("https://contoso.vault.azure.net/");
var sb = AzureBlastFactory.CreateServiceBus("<sb-connection-string>", "orders");
```

---

## 🧪 Using the services

> All interfaces live under `AzureBlast.Interfaces`.

### SQL (IMssqlDatabase)

```csharp
using AzureBlast.Interfaces;

var db = sp.GetRequiredService<IMssqlDatabase>();

var rowsAffected = db.ExecuteNonQuery(
    "UPDATE Users SET IsActive = @active WHERE Id = @id",
    new() { ["@active"] = true, ["@id"] = 42 });

var result = db.ExecuteScalar(
    "SELECT COUNT(*) FROM Users WHERE IsActive = @active",
    new() { ["@active"] = true });
```

### Service Bus (IAzureServiceBus)

```csharp
using AzureBlast.Interfaces;

// Resolve from DI (already configured via options/fluent)
var bus = sp.GetRequiredService<IAzureServiceBus>();

// Send a message
await bus.SendMessageAsync("""{ "type": "hello", "payload": "AzureBlast" }"" );

// Receive some messages
var received = await bus.ReceiveMessagesAsync(maxMessages: 10);
foreach (var msg in received ?? [])
{
    // ...process...
    await bus.CompleteMessageAsync(msg);
}
```

### Key Vault (IAzureKeyVault)

```csharp
using AzureBlast.Interfaces;

var kv = sp.GetRequiredService<IAzureKeyVault>();

await kv.SetSecretAsync("MySecret", "shh");
var value = await kv.GetSecretAsync("MySecret");
// value == "shh"
```

### Table Storage (IAzureTableStorage)

```csharp
using AzureBlast.Interfaces;
using Azure.Data.Tables;

var tables = sp.GetRequiredService<IAzureTableStorage>();

// Optional: switch table at runtime
tables.SetTable("MyTable");

// Define an entity
public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Users";
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

// Upsert
await tables.UpsertEntityAsync(new UserEntity { Name = "Ada", IsActive = true });

// Query
var users = await tables.QueryEntitiesAsync<UserEntity>("IsActive eq true");

// Get & Delete
var first = users.FirstOrDefault();
if (first is not null)
{
    var fetched = await tables.GetEntityAsync<UserEntity>(first.PartitionKey, first.RowKey);
    await tables.DeleteEntityAsync(first.PartitionKey, first.RowKey);
}
```

### ARM / Resource Graph (IAzureResourceClient)

```csharp
using AzureBlast.Interfaces;

// Always available; wrapper is registered by default.
var rc = sp.GetRequiredService<IAzureResourceClient>();

// List subscriptions and set the one you want
var subs = await rc.ListSubscriptionsAsync();
var subId = subs.First().Id.SubscriptionId!;
await rc.SetSubscriptionContextAsync(subId);

// Explore resources
var groups = await rc.GetResourceGroupsByTagAsync("env", "prod");
var vms    = await rc.GetResourcesByTypeAsync("Microsoft.Compute/virtualMachines");
var exists = await rc.ResourceExistsAsync("rg-app", "my-app-plan");
```

---

## 🧪 Testing

* The DI registration via options uses `TryAdd*` so repeated calls **don’t duplicate** registrations and **won’t overwrite** your own.
* Service Bus, Table Storage, and ARM are exposed through interfaces and adapters so you can stub or mock them in unit tests.
* Example: provide a fake `IAzureTableStorage` in the test container and assert it’s preserved when calling `AddAzureBlast`.

---

## 📖 API docs

XML documentation is included with the package and covers all public types:

* `ServiceCollectionExtensions.AddAzureBlast(...)` (options-based)
* `AzureBlastRegistration.UseAzureBlast(...)` (fluent builder)
* `AzureBlastFactory` (script/no-DI helpers)
* `IMssqlDatabase`, `IAzureServiceBus`, `IAzureKeyVault`, `IAzureTableStorage`, `IAzureResourceClient`, etc.

---

## 📝 Notes

* **Credentials**: If you don’t pass a `TokenCredential`, `DefaultAzureCredential` is used.
* **Key Vault**: The client is initialized during registration via `InitializeKeyVaultAsync(vaultUrl)` so it’s ready to use.
* **Table Storage**: You can set a default table during registration and change it later with `SetTable(...)`.

---

## 🔧 Minimal quick-start (Service Bus only)

```csharp
using AzureBlast;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAzureBlast(o =>
{
    o.ServiceBusConnectionString = "<sb-connection-string>";
    o.ServiceBusQueueName = "queue1";
});

var sp  = services.BuildServiceProvider();
var bus = sp.GetRequiredService<AzureBlast.Interfaces.IAzureServiceBus>();

await bus.SendMessageAsync("""{ "hello": "world" }""");
```
