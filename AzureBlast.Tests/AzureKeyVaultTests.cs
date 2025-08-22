using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Moq;

namespace AzureBlast.Tests;

public class AzureKeyVaultTests
{
    private readonly AzureKeyVault _keyVault;
    private readonly Mock<SecretClient> _mockClient;

    public AzureKeyVaultTests()
    {
        _mockClient = new Mock<SecretClient>();

        // Inject mock through the factory; a production path will call it in InitializeKeyVaultAsync
        _keyVault = new AzureKeyVault(
            Mock.Of<TokenCredential>(),
            (_, _) => _mockClient.Object
        );

        // Exercise the real initialization path
        _keyVault.InitializeKeyVaultAsync("https://example.vault.azure.net/").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ListSecretsAsync_ReturnsSecretNames()
    {
        var mockSecrets = GetMockSecrets(["foo", "bar"]);
        _mockClient
            .Setup(c => c.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()))
            .Returns(mockSecrets);

        var result = await _keyVault.ListSecretsAsync();

        Assert.Contains("foo", result);
        Assert.Contains("bar", result);
    }

    [Fact]
    public async Task GetSecretAsync_ReturnsSecretValue()
    {
        var secret = new KeyVaultSecret("foo", "bar");
        var mockResponse = Response.FromValue(secret, Mock.Of<Response>());

        _mockClient
            .Setup(c => c.GetSecretAsync("foo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        var value = await _keyVault.GetSecretAsync("foo");

        Assert.Equal("bar", value);
    }

    [Fact]
    public async Task SetSecretAsync_CallsClient()
    {
        _mockClient
            .Setup(c => c.SetSecretAsync("foo", "bar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("foo", "bar"), Mock.Of<Response>()));

        await _keyVault.SetSecretAsync("foo", "bar");

        _mockClient.Verify(c => c.SetSecretAsync("foo", "bar", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSecretAsync_WaitsForCompletion()
    {
        var mockOp = new FakeDeleteSecretOperation();

        _mockClient
            .Setup(c => c.StartDeleteSecretAsync("foo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOp);

        await _keyVault.DeleteSecretAsync("foo");

        _mockClient.Verify(c => c.StartDeleteSecretAsync("foo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeSecretAsync_CallsClient()
    {
        _mockClient
            .Setup(c => c.PurgeDeletedSecretAsync("foo", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<Response>()));

        await _keyVault.PurgeSecretAsync("foo");

        _mockClient.Verify(c => c.PurgeDeletedSecretAsync("foo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverDeletedSecretAsync_CallsClient()
    {
        var mockOp = new Mock<RecoverDeletedSecretOperation>();
        _mockClient
            .Setup(c => c.StartRecoverDeletedSecretAsync("foo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOp.Object);

        await _keyVault.RecoverDeletedSecretAsync("foo");

        _mockClient.Verify(c => c.StartRecoverDeletedSecretAsync("foo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Methods_ThrowIfClientNotInitialized()
    {
        var uninitialized = new AzureKeyVault(Mock.Of<TokenCredential>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.ListSecretsAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.GetSecretAsync("test"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.SetSecretAsync("test", "val"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.DeleteSecretAsync("test"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.PurgeSecretAsync("test"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized.RecoverDeletedSecretAsync("test"));
    }

    private static AsyncPageable<SecretProperties> GetMockSecrets(IEnumerable<string> names)
    {
        var items = names.Select(name =>
            SecretModelFactory.SecretProperties(
                id: new Uri($"https://example.vault.azure.net/secrets/{name}"),
                name: name
            ));

        return new TestAsyncPageable<SecretProperties>(items.ToList());
    }

    private class TestAsyncPageable<T> : AsyncPageable<T> where T : notnull
    {
        private readonly IReadOnlyList<T> _items;

        public TestAsyncPageable(IReadOnlyList<T> items)
        {
            _items = items;
        }

        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            yield return Page<T>.FromValues(_items, null, Mock.Of<Response>());
            await Task.CompletedTask;
        }
    }

    private class FakeDeleteSecretOperation : DeleteSecretOperation
    {
        private readonly DeletedSecret _value;

        public FakeDeleteSecretOperation()
        {
            // Build SecretProperties for the factory
            var props = SecretModelFactory.SecretProperties(
                id: new Uri("https://example.vault.azure.net/secrets/foo"),
                name: "foo");

            // Correct parameter names: deletedOn, scheduledPurgeDate
            _value = SecretModelFactory.DeletedSecret(
                properties: props,
                value: null,
                recoveryId: null,
                deletedOn: null,
                scheduledPurgeDate: null);
        }

        public override string Id => "fake-op-id";
        public override bool HasCompleted => true;
        public override bool HasValue => true;
        public override DeletedSecret Value => _value;

        public override Response GetRawResponse() => Mock.Of<Response>();

        public override ValueTask<Response<DeletedSecret>> WaitForCompletionAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Response.FromValue(_value, GetRawResponse()));

        public override ValueTask<Response<DeletedSecret>> WaitForCompletionAsync(
            TimeSpan pollingInterval,
            CancellationToken cancellationToken)
            => WaitForCompletionAsync(cancellationToken);
    }
}
