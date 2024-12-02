// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Server.Circuits;
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

    private static (Mock<ISingleClientProxy>, ComponentHub) InitializeComponentHub()
    {
        var ephemeralDataProtectionProvider = new EphemeralDataProtectionProvider();
        var circuitIdFactory = new CircuitIdFactory(ephemeralDataProtectionProvider);
        var circuitFactory = new TestCircuitFactory(
            new Mock<IServiceScopeFactory>().Object,
            NullLoggerFactory.Instance,
            circuitIdFactory,
            Options.Create(new CircuitOptions()));
        var circuitRegistry = new CircuitRegistry(
            Options.Create(new CircuitOptions()),
            NullLogger<CircuitRegistry>.Instance,
            circuitIdFactory);
        var serializer = new TestServerComponentDeserializer();
        var circuitHandleRegistry = new TestCircuitHandleRegistry();
        var hub = new ComponentHub(
            serializer: serializer,
            dataProtectionProvider: ephemeralDataProtectionProvider,
            circuitFactory: circuitFactory,
            circuitIdFactory: circuitIdFactory,
            circuitRegistry: circuitRegistry,
            circuitHandleRegistry: circuitHandleRegistry,
            logger: NullLogger<ComponentHub>.Instance);

        // Here we mock out elements of the Hub that are typically configured
        // by SignalR as clients connect to the hub.
        var mockCaller = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockCaller.Setup(x => x.Caller).Returns(mockClientProxy.Object);
        hub.Clients = mockCaller.Object;
        var mockContext = new Mock<HubCallerContext>();
        var feature = new FeatureCollection();
        var httpContextFeature = new Mock<IHttpContextFeature>();
        httpContextFeature.Setup(x => x.HttpContext).Returns(() => new DefaultHttpContext());
        feature.Set(httpContextFeature.Object);
        mockContext.Setup(x => x.Features).Returns(feature);
        mockContext.Setup(x => x.ConnectionId).Returns("123");
        hub.Context = mockContext.Object;

        return (mockClientProxy, hub);
    }

    private class TestCircuitHandleRegistry : ICircuitHandleRegistry
    {
        private bool circuitSet = false;

        public CircuitHandle GetCircuitHandle(IDictionary<object, object> circuitHandles, object circuitKey)
        {
            return null;
        }

        public CircuitHost GetCircuit(IDictionary<object, object> circuitHandles, object circuitKey)
        {
            if (circuitSet)
            {
                var serviceScope = new Mock<IServiceScope>();
                var circuitHost = TestCircuitHost.Create(
                    serviceScope: new AsyncServiceScope(serviceScope.Object));
                return circuitHost;
            }
            return null;
        }

        public void SetCircuit(IDictionary<object, object> circuitHandles, object circuitKey, CircuitHost circuitHost)
        {
            circuitSet = true;
            return;
        }
    }

    private class TestServerComponentDeserializer : IServerComponentDeserializer
    {
        public bool TryDeserializeComponentDescriptorCollection(string serializedComponentRecords, out List<ComponentDescriptor> descriptors)
        {
            descriptors = default;
            return true;
        }

        public bool TryDeserializeRootComponentOperations(string serializedComponentOperations, out RootComponentOperationBatch operationsWithDescriptors)
        {
            operationsWithDescriptors = default;
            return true;
        }
    }

    private class TestCircuitFactory : ICircuitFactory
    {
        public TestCircuitFactory(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        CircuitIdFactory circuitIdFactory,
        IOptions<CircuitOptions> options)
        { }

        // Implement a `CreateCircuitHostAsync` that mocks the construction
        // of the CircuitHost.
        public ValueTask<CircuitHost> CreateCircuitHostAsync(
            IReadOnlyList<ComponentDescriptor> components,
            CircuitClientProxy client,
            string baseUri,
            string uri,
            ClaimsPrincipal user,
            IPersistentComponentStateStore store,
            ResourceAssetCollection resourceCollection)
        {
            var serviceScope = new Mock<IServiceScope>();
            var circuitHost = TestCircuitHost.Create(serviceScope: new AsyncServiceScope(serviceScope.Object));
            return ValueTask.FromResult(circuitHost);
        }
    }
}
