# AzureBlast

**AzureBlast** is a lightweight,
injectable .NET library that simplifies working with **Azure services**
(such as **Azure SQL**, **Service Bus**, and **Key Vault**) in applications,
scripts, and tools.
It is designed to be consumed from **C# projects**, **LINQPad queries**, or even **PowerShell**,
giving you a consistent and testable interface to Azure resources.

### ✨ Features

* **MSSQL Database Access** – strongly typed wrapper for queries, commands, and transactions.
* **Azure Service Bus** – simple message publishing, batching, scheduling, and receiving.
* **Azure Key Vault** – easy secret retrieval and updates.
* **Dependency Injection Friendly** – clean interfaces for plugging into your app or services.
* **LINQPad & PowerShell Ready** – usable in quick scripts as well as production code.
* **Test Coverage** – designed with mocks and unit testing in mind.

### 📦 Installation

Available as a **NuGet package**:

```sh
dotnet add package AzureBlast
```

### 🚀 Example Usage

```csharp
// Inject or create an AzureServiceBus
var bus = new AzureServiceBus(client);
bus.Setup(connectionString, "queue1");

// Send a message
await bus.SendMessageAsync("Hello from AzureBlast!");

// Receive messages
var messages = await bus.ReceiveMessagesAsync();
```
