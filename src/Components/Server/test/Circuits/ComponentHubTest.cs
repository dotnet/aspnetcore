// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server;

public class ComponentHubTest
{
    [Fact]
    public async Task CannotStartMultipleCircuits()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        Assert.NotNull(circuitSecret);

        var circuit2Secret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        Assert.Null(circuit2Secret);

        var errorMessage = "The circuit host '.*?' has already been initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", It.Is<object[]>(s => Regex.Match((string)s[0], errorMessage).Success), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task StartCircuitFailsWithNullData()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();
        var circuitSecret = await hub.StartCircuit(null, null, "foo", null);

        Assert.Null(circuitSecret);
        var errorMessage = "The uris provided are invalid.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void GetStartupValueKeysReturnsProviderKeysInRegistrationOrder()
    {
        var providers = new IBrowserStartupValueProvider[]
        {
            new TestBrowserStartupValueProvider("first.value"),
            new TestBrowserStartupValueProvider("second.value"),
        };
        var (_, hub) = InitializeComponentHub(browserStartupValueProviders: providers);

        Assert.Equal(["first.value", "second.value"], hub.GetStartupValueKeys());
    }

    [Fact]
    public void GetStartupValueKeysRejectsDuplicates()
    {
        var providers = new IBrowserStartupValueProvider[]
        {
            new TestBrowserStartupValueProvider("duplicate.value"),
            new TestBrowserStartupValueProvider("duplicate.value"),
        };
        var (_, hub) = InitializeComponentHub(browserStartupValueProviders: providers);

        var exception = Assert.Throws<InvalidOperationException>(() => hub.GetStartupValueKeys());
        Assert.Equal("The browser startup value key 'duplicate.value' was provided more than once.", exception.Message);
    }

    [Fact]
    public async Task StartCircuitLegacyPathSeedsNavigationStartupValues()
    {
        var circuitFactory = new TestCircuitFactory();
        var (_, hub) = InitializeComponentHub(circuitFactory: circuitFactory);

        var circuitSecret = await hub.StartCircuit(
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "{}",
            null);

        Assert.NotNull(circuitSecret);
        Assert.Equal("https://localhost:5000/", circuitFactory.StartupValues["document.baseURI"]);
        Assert.Equal("https://localhost:5000/page", circuitFactory.StartupValues["location.href"]);
        Assert.Equal(2, circuitFactory.StartupValues.Count);
    }

    [Fact]
    public async Task StartCircuitWithStartupValuesPassesValidatedValues()
    {
        var circuitFactory = new TestCircuitFactory();
        var (_, hub) = InitializeComponentHub(
            circuitFactory: circuitFactory,
            browserStartupValueProviders:
            [
                new TestBrowserStartupValueProvider("document.baseURI", "location.href", "custom.value"),
            ]);

        var circuitSecret = await hub.StartCircuitWithStartupValues(
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "{}",
            null,
            """{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/page","custom.value":"expected"}""");

        Assert.NotNull(circuitSecret);
        Assert.Equal("expected", circuitFactory.StartupValues["custom.value"]);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"value":"first","value":"second"}""")]
    [InlineData("""{"value":42}""")]
    [InlineData("""{"document.baseURI":"https://other.example/","location.href":"https://localhost:5000/page"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/other"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/page","unexpected":"value"}""")]
    public async Task StartCircuitWithStartupValuesRejectsInvalidValues(string startupValuesJson)
    {
        var (mockClientProxy, hub) = InitializeComponentHub(
            browserStartupValueProviders:
            [
                new TestBrowserStartupValueProvider("document.baseURI", "location.href"),
            ]);

        var circuitSecret = await hub.StartCircuitWithStartupValues(
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "{}",
            null,
            startupValuesJson);

        Assert.Null(circuitSecret);
        mockClientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "JS.Error",
                new[] { "The startup values provided are invalid." },
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CannotInvokeJSInteropBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();

        await hub.BeginInvokeDotNetFromJS("", "", "", 0, "");

        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotInvokeJSInteropCallbackCompletionsBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();

        await hub.EndInvokeJSFromDotNet(3, true, "[]");

        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotInvokeOnRenderCompletedBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();

        await hub.OnRenderCompleted(5, null);

        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotInvokeOnLocationChangedBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();

        await hub.OnLocationChanged("https://localhost:5000/subdir/page", null, false);

        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotInvokeOnLocationChangingBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();

        await hub.OnLocationChanging(0, "https://localhost:5000/subdir/page", null, false);

        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotCallUpdateRootComponentsBeforeInitialization()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();
        await hub.UpdateRootComponents("""{ batchId: 1, operations: [] }""", "");
        var errorMessage = "Circuit not initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CanCallUpdateRootComponents()
    {
        var called = false;
        var deserializer = new TestServerComponentDeserializer();
        deserializer.OnTryDeserializeTestComponentOperations =
            (serializedComponentOperations, out operationsWithDescriptors, deserializeDescriptors) =>
            {
                called = true;
                operationsWithDescriptors = new RootComponentOperationBatch
                {
                    BatchId = 1,
                    Operations = []
                };
                return true;
            };
        var (mockClientProxy, hub) = InitializeComponentHub(deserializer);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "[]", null);
        Assert.NotNull(circuitSecret);
        await hub.UpdateRootComponents("""{ batchId: 1, operations: [] }""", "");
        Assert.True(called);
    }

    [Fact]
    public async Task CanCallUpdateRootComponentsOnResumedCircuit()
    {
        var deserializer = new TestServerComponentDeserializer();
        deserializer.OnTryDeserializeTestComponentOperations =
            (serializedComponentOperations, out operationsWithDescriptors, deserializeDescriptors) =>
            {
                operationsWithDescriptors = new RootComponentOperationBatch
                {
                    BatchId = 1,
                    Operations = []
                };
                return true;
            };

        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        CircuitHost lastCircuit = null;
        handleRegistryMock.Setup(m => m.SetCircuit(It.IsAny<IDictionary<object, object>>(), It.IsAny<object>(), It.IsAny<CircuitHost>()))
            .Callback<IDictionary<object, object>, object, CircuitHost>((circuitHandles, circuitKey, circuitHost) =>
            {
                lastCircuit = circuitHost;
            });
        handleRegistryMock.Setup(m => m.GetCircuit(It.IsAny<IDictionary<object, object>>(), It.IsAny<object>()))
            .Returns(() => lastCircuit);
        handleRegistryMock.Setup(m => m.GetCircuitHandle(It.IsAny<IDictionary<object, object>>(), It.IsAny<object>()))
            .Returns(() => lastCircuit.Handle);

        var providerMock = new Mock<ICircuitPersistenceProvider>();
        providerMock.Setup(m => m.RestoreCircuitAsync(It.IsAny<CircuitId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistedCircuitState
            {
                RootComponents = [.. """{}"""u8],
                ApplicationState = ReadOnlyDictionary<string, byte[]>.Empty
            });

        var (mockClientProxy, hub) = InitializeComponentHub(deserializer, handleRegistryMock.Object, providerMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "[]", null);
        lastCircuit = null;
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", "[]", "");
        await hub.UpdateRootComponents("""{ batchId: 1, operations: [] }""", "");
        Assert.False(lastCircuit.HasPendingPersistedCircuitState);
    }

    [Fact]
    public async Task CannotCallResumeCircuitWithInvalidId()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();
        var invalidCircuitId = "invalid-circuit-id";
        var result = await hub.ResumeCircuit(invalidCircuitId, null, null, null, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task CannotResumeConnectedCircuit()
    {
        var (mockClientProxy, hub) = InitializeComponentHub();
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        Assert.NotNull(circuitSecret);
        var result = await hub.ResumeCircuit(circuitSecret, null, null, null, null);
        Assert.Null(result);
        var errorMessage = "The circuit host '.*?' has already been initialized.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", It.Is<object[]>(s => Regex.Match((string)s[0], errorMessage).Success), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotResumeInvalidUrls()
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var (mockClientProxy, hub) = InitializeComponentHub(null, handleRegistryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, null, null, null, null);
        Assert.Null(result);
        var errorMessage = "The uris provided are invalid.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CannotResumeWithRootComponentsButWithoutAppState(string appState)
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var (mockClientProxy, hub) = InitializeComponentHub(null, handleRegistryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", "unused", appState);
        Assert.Null(result);
        var errorMessage = "The application state provided is invalid.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    public async Task CannotResumeWithAppStateButWithoutRootComponents(string rootComponents)
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var (mockClientProxy, hub) = InitializeComponentHub(null, handleRegistryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", rootComponents, "app-state");
        Assert.Null(result);
        var errorMessage = "The root components provided are invalid.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CannotResumeAppWhenPersistedComponentStateIsNotAvailable()
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var (mockClientProxy, hub) = InitializeComponentHub(null, handleRegistryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", "[]", "");
        Assert.Null(result);
    }

    [Fact]
    public async Task CanResumeAppWhenPersistedComponentStateIsAvailable()
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        CircuitHost lastCircuit = null;
        handleRegistryMock.Setup(m => m.SetCircuit(It.IsAny<IDictionary<object, object>>(), It.IsAny<object>(), It.IsAny<CircuitHost>()))
            .Callback<IDictionary<object, object>, object, CircuitHost>((circuitHandles, circuitKey, circuitHost) =>
            {
                lastCircuit = circuitHost;
            });
        var providerMock = new Mock<ICircuitPersistenceProvider>();
        providerMock.Setup(m => m.RestoreCircuitAsync(It.IsAny<CircuitId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistedCircuitState
            {
                RootComponents = [.. """{}"""u8],
                ApplicationState = ReadOnlyDictionary<string, byte[]>.Empty,
            });

        var (mockClientProxy, hub) = InitializeComponentHub(null, handleRegistryMock.Object, providerMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", "[]", "");
        Assert.NotNull(result);
        Assert.NotEqual(circuitSecret, result);
        Assert.True(lastCircuit.HasPendingPersistedCircuitState);
    }

    [Fact]
    public async Task ResumeCircuitWithStartupValuesPassesValidatedValues()
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var providerMock = new Mock<ICircuitPersistenceProvider>();
        providerMock.Setup(m => m.RestoreCircuitAsync(It.IsAny<CircuitId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistedCircuitState
            {
                RootComponents = [.. """{}"""u8],
                ApplicationState = ReadOnlyDictionary<string, byte[]>.Empty,
            });
        var circuitFactory = new TestCircuitFactory();
        var (_, hub) = InitializeComponentHub(
            handleRegistry: handleRegistryMock.Object,
            provider: providerMock.Object,
            circuitFactory: circuitFactory,
            browserStartupValueProviders:
            [
                new TestBrowserStartupValueProvider("document.baseURI", "location.href", "custom.value"),
            ]);
        var circuitSecret = await hub.StartCircuit(
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "{}",
            null);

        var result = await hub.ResumeCircuitWithStartupValues(
            circuitSecret,
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "[]",
            "",
            """{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/page","custom.value":"expected"}""");

        Assert.NotNull(result);
        Assert.Equal("expected", circuitFactory.StartupValues["custom.value"]);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"value":"first","value":"second"}""")]
    [InlineData("""{"value":42}""")]
    [InlineData("""{"document.baseURI":"https://other.example/","location.href":"https://localhost:5000/page"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/other"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/"}""")]
    [InlineData("""{"document.baseURI":"https://localhost:5000/","location.href":"https://localhost:5000/page","unexpected":"value"}""")]
    public async Task ResumeCircuitWithStartupValuesRejectsInvalidValues(string startupValuesJson)
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var (mockClientProxy, hub) = InitializeComponentHub(
            handleRegistry: handleRegistryMock.Object,
            browserStartupValueProviders:
            [
                new TestBrowserStartupValueProvider("document.baseURI", "location.href"),
            ]);
        var circuitSecret = await hub.StartCircuit(
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "{}",
            null);

        var result = await hub.ResumeCircuitWithStartupValues(
            circuitSecret,
            "https://localhost:5000/",
            "https://localhost:5000/page",
            "[]",
            "",
            startupValuesJson);

        Assert.Null(result);
        mockClientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "JS.Error",
                new[] { "The startup values provided are invalid." },
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task StartCircuitFailsWithUnresolvedCircuitHandlerDependency_NotifiesClientToCheckServerLogs()
    {
        var circuitFactoryMock = new Mock<ICircuitFactory>();
        circuitFactoryMock
            .Setup(m => m.CreateCircuitHostAsync(
                It.IsAny<IReadOnlyList<ComponentDescriptor>>(),
                It.IsAny<CircuitClientProxy>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<IPersistentComponentStateStore>(),
                It.IsAny<ResourceAssetCollection>()))
            .ThrowsAsync(new InvalidOperationException("Unable to resolve service for type 'IMyUnresolvedDependency'."));

        var (mockClientProxy, hub) = InitializeComponentHub(circuitFactory: circuitFactoryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);

        Assert.Null(circuitSecret);
        var errorMessage = "The circuit failed to initialize. See the server logs for more information.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ResumeCircuitFailsWithUnresolvedCircuitHandlerDependency_NotifiesClientToCheckServerLogs()
    {
        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        var providerMock = new Mock<ICircuitPersistenceProvider>();
        providerMock.Setup(m => m.RestoreCircuitAsync(It.IsAny<CircuitId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistedCircuitState
            {
                RootComponents = [.. """{}"""u8],
                ApplicationState = ReadOnlyDictionary<string, byte[]>.Empty,
            });

        var circuitFactoryMock = new Mock<ICircuitFactory>();
        circuitFactoryMock
            .Setup(m => m.CreateCircuitHostAsync(
                It.IsAny<IReadOnlyList<ComponentDescriptor>>(),
                It.IsAny<CircuitClientProxy>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<IPersistentComponentStateStore>(),
                It.IsAny<ResourceAssetCollection>()))
            .ThrowsAsync(new InvalidOperationException("Unable to resolve service for type 'IMyUnresolvedDependency'."));

        var (mockClientProxy, hub) = InitializeComponentHub(
            deserializer: null,
            handleRegistry: handleRegistryMock.Object,
            provider: providerMock.Object,
            circuitFactory: circuitFactoryMock.Object);
        var circuitSecret = await hub.StartCircuit("https://localhost:5000", "https://localhost:5000/subdir", "{}", null);
        var result = await hub.ResumeCircuit(circuitSecret, "https://localhost:5000", "https://localhost:5000/subdir", "[]", "");

        Assert.Null(result);
        var errorMessage = "The circuit failed to initialize. See the server logs for more information.";
        mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task OnConnectedAsyncReplacesSignalRUserRefreshPolicy()
    {
        var userRefreshFeature = new Mock<IConnectionAuthenticationRefreshFeature>();
        userRefreshFeature.SetupAllProperties();
        userRefreshFeature.Object.OnAuthenticationRefresh = static _ => Task.FromResult(false);
        var (_, hub) = InitializeComponentHub(userRefreshFeature: userRefreshFeature.Object);

        await hub.OnConnectedAsync();

        var callback = userRefreshFeature.Object.OnAuthenticationRefresh;
        Assert.NotNull(callback);
        var context = new AuthenticationRefreshContext
        {
            HttpContext = new DefaultHttpContext(),
            ConnectionId = "123",
            PreviousUser = new ClaimsPrincipal(),
            NewUser = new ClaimsPrincipal(),
            NewExpiration = null,
        };
        Assert.True(await callback(context));
    }

    [Fact]
    public async Task OnAuthenticationRefreshedAsyncUpdatesCircuitUser()
    {
        var authenticationStateProvider = new ServerAuthenticationStateProvider();
        var services = new ServiceCollection()
            .AddSingleton<AuthenticationStateProvider>(authenticationStateProvider)
            .BuildServiceProvider();
        var circuitHost = TestCircuitHost.Create(serviceScope: services.CreateAsyncScope());

        var handleRegistryMock = new Mock<ICircuitHandleRegistry>();
        handleRegistryMock.Setup(m => m.GetCircuit(It.IsAny<IDictionary<object, object>>(), It.IsAny<object>()))
            .Returns(circuitHost);

        var refreshedUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "refreshed-user")],
            "TestAuthType"));
        var (_, hub) = InitializeComponentHub(handleRegistry: handleRegistryMock.Object, user: refreshedUser);

        await hub.OnAuthenticationRefreshedAsync();

        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        Assert.Same(refreshedUser, authenticationState.User);
    }

    [Fact]
    public async Task OnAuthenticationRefreshedAsyncWithoutCircuitDoesNotThrow()
    {
        var (_, hub) = InitializeComponentHub();

        await hub.OnAuthenticationRefreshedAsync();
    }

    private static (Mock<ISingleClientProxy>, ComponentHub) InitializeComponentHub(
        TestServerComponentDeserializer deserializer = null,
        ICircuitHandleRegistry handleRegistry = null,
        ICircuitPersistenceProvider provider = null,
        ICircuitFactory circuitFactory = null,
        ClaimsPrincipal user = null,
        IConnectionAuthenticationRefreshFeature userRefreshFeature = null,
        IEnumerable<IBrowserStartupValueProvider> browserStartupValueProviders = null)
    {
        deserializer ??= new TestServerComponentDeserializer();
        var ephemeralDataProtectionProvider = new EphemeralDataProtectionProvider();
        var circuitPersistenceManager = new CircuitPersistenceManager(
            Options.Create(new CircuitOptions()),
            new Endpoints.ServerComponentSerializer(ephemeralDataProtectionProvider),
            provider ?? Mock.Of<ICircuitPersistenceProvider>(),
            ephemeralDataProtectionProvider);

        var circuitIdFactory = TestCircuitIdFactory.Instance;
        var circuitFactoryInstance = circuitFactory ?? new TestCircuitFactory(
            new Mock<IServiceScopeFactory>().Object,
            NullLoggerFactory.Instance,
            circuitIdFactory,
            Options.Create(new CircuitOptions()));
        var circuitRegistry = new CircuitRegistry(
            Options.Create(new CircuitOptions()),
            NullLogger<CircuitRegistry>.Instance,
            circuitIdFactory, circuitPersistenceManager);
        var circuitHandleRegistry = handleRegistry ?? new TestCircuitHandleRegistry();
        var hub = new ComponentHub(
            serializer: deserializer,
            dataProtectionProvider: ephemeralDataProtectionProvider,
            circuitFactory: circuitFactoryInstance,
            circuitIdFactory: circuitIdFactory,
            circuitRegistry: circuitRegistry,
            circuitPersistenceProvider: circuitPersistenceManager,
            circuitHandleRegistry: circuitHandleRegistry,
            browserStartupValueProviders: browserStartupValueProviders ?? [],
            logger: NullLogger<ComponentHub>.Instance);

        // Here we mock out elements of the Hub that are typically configured
        // by SignalR as clients connect to the hub.
        var mockCaller = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockCaller.Setup(x => x.Caller).Returns(mockClientProxy.Object);
        hub.Clients = mockCaller.Object;
        var mockContext = new Mock<HubCallerContext>();
        var items = new Dictionary<object, object>();
        mockContext.Setup(x => x.Items).Returns(items);
        var feature = new FeatureCollection();
        var httpContextFeature = new Mock<IHttpContextFeature>();
        httpContextFeature.Setup(x => x.HttpContext).Returns(() => new DefaultHttpContext());
        feature.Set(httpContextFeature.Object);
        if (userRefreshFeature is not null)
        {
            feature.Set(userRefreshFeature);
        }
        mockContext.Setup(x => x.Features).Returns(feature);
        mockContext.Setup(x => x.ConnectionId).Returns("123");
        mockContext.Setup(x => x.User).Returns(user ?? new ClaimsPrincipal());
        hub.Context = mockContext.Object;

        return (mockClientProxy, hub);
    }

    private class TestCircuitHandleRegistry : ICircuitHandleRegistry
    {
        private bool circuitSet = false;
        private CircuitHost _circuitHost;
        private CircuitHandle _circuitHandle;

        public CircuitHandle GetCircuitHandle(IDictionary<object, object> circuitHandles, object circuitKey)
        {
            return _circuitHandle;
        }

        public CircuitHost GetCircuit(IDictionary<object, object> circuitHandles, object circuitKey)
        {
            if (circuitSet)
            {
                return _circuitHost;
            }
            return null;
        }

        public void SetCircuit(IDictionary<object, object> circuitHandles, object circuitKey, CircuitHost circuitHost)
        {
            circuitSet = true;
            _circuitHost = circuitHost;
            _circuitHandle = new CircuitHandle { CircuitHost = circuitHost };

            return;
        }
    }

    private class TestServerComponentDeserializer : IServerComponentDeserializer
    {
        public delegate bool TestTryDeserializeRootComponentOperations(string serializedComponentOperations, out RootComponentOperationBatch operationsWithDescriptors, bool deserializeDescriptors = true);
        public delegate bool TestTryDeserializeWebRootComponentDescriptor(ComponentMarker record, [NotNullWhen(true)] out WebRootComponentDescriptor result);

        public TestTryDeserializeRootComponentOperations OnTryDeserializeTestComponentOperations { get; set; }

        public bool TryDeserializeComponentDescriptorCollection(string serializedComponentRecords, out List<ComponentDescriptor> descriptors)
        {
            descriptors = default;
            return true;
        }

        public bool TryDeserializeRootComponentOperations(string serializedComponentOperations, out RootComponentOperationBatch operationsWithDescriptors, bool deserializeDescriptors = true)
        {
            if (OnTryDeserializeTestComponentOperations != null)
            {
                return OnTryDeserializeTestComponentOperations(serializedComponentOperations, out operationsWithDescriptors, deserializeDescriptors);
            }
            else
            {
                operationsWithDescriptors = default;
                return true;
            }
        }

        public bool TryDeserializeWebRootComponentDescriptor(ComponentMarker record, [NotNullWhen(true)] out WebRootComponentDescriptor result)
        {
            result = default;
            return true;
        }
    }

    private class TestCircuitFactory : ICircuitFactory
    {
        public TestCircuitFactory()
        {
        }

        public TestCircuitFactory(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        CircuitIdFactory circuitIdFactory,
        IOptions<CircuitOptions> options)
        { }

        public IReadOnlyDictionary<string, string> StartupValues { get; private set; } =
            new Dictionary<string, string>();

        // Implement a `CreateCircuitHostAsync` that mocks the construction
        // of the CircuitHost.
        public ValueTask<CircuitHost> CreateCircuitHostAsync(
            IReadOnlyList<ComponentDescriptor> components,
            CircuitClientProxy client,
            string baseUri,
            string uri,
            IReadOnlyDictionary<string, string> startupValues,
            ClaimsPrincipal user,
            IPersistentComponentStateStore store,
            ResourceAssetCollection resourceCollection)
        {
            StartupValues = startupValues;
            var clientProxy = new CircuitClientProxy(Mock.Of<ISingleClientProxy>(), "123");

            var serviceScope = new Mock<IServiceScope>();
            var circuitHost = TestCircuitHost.Create(
                circuitId: TestCircuitIdFactory.Instance.CreateCircuitId(),
                serviceScope: new AsyncServiceScope(serviceScope.Object),
                clientProxy: clientProxy);
            return ValueTask.FromResult(circuitHost);
        }
    }

    private sealed class TestBrowserStartupValueProvider(params string[] keys) : IBrowserStartupValueProvider
    {
        public IReadOnlyList<string> Keys { get; } = keys;
    }
}
