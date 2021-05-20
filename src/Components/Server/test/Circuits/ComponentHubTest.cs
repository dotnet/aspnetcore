// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Components.Lifetime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Security.Claims;
using Moq;
using Xunit;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
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
        public async Task CannotDispatchBrowserEventsBeforeInitialization()
        {
            var (mockClientProxy, hub) = InitializeComponentHub();

            await hub.DispatchBrowserEvent("", "");

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

            await hub.OnLocationChanged("https://localhost:5000/subdir/page", false);

            var errorMessage = "Circuit not initialized.";
            mockClientProxy.Verify(m => m.SendCoreAsync("JS.Error", new[] { errorMessage }, It.IsAny<CancellationToken>()), Times.Once());
        }

        private static (Mock<IClientProxy>, ComponentHub) InitializeComponentHub()
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
            var serializer = new Mock<ServerComponentDeserializer>(
                ephemeralDataProtectionProvider,
                NullLogger<ServerComponentDeserializer>.Instance,
                new RootComponentTypeCache(),
                new ComponentParameterDeserializer(
                    NullLogger<ComponentParameterDeserializer>.Instance,
                    new ComponentParametersTypeCache()));
            var hub = new TestComponentHub(
                serializer: serializer.Object,
                dataProtectionProvider: ephemeralDataProtectionProvider,
                circuitFactory: circuitFactory,
                circuitIdFactory: circuitIdFactory,
                circuitRegistry: circuitRegistry,
                logger: NullLogger<ComponentHub>.Instance);

            // Here we mock out elements of the Hub that are typically configured
            // by SignalR as clients connect to the hub.
            var mockCaller = new Mock<IHubCallerClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockCaller.Setup(x => x.Caller).Returns(mockClientProxy.Object);
            hub.Clients = mockCaller.Object;
            var mockContext = new Mock<HubCallerContext>();
            mockContext.Setup(x => x.ConnectionId).Returns("123");
            hub.Context = mockContext.Object;

            return (mockClientProxy, hub);
        }

        private class TestComponentHub : ComponentHub
        {
            private bool circuitSet = false;

            public TestComponentHub(
                ServerComponentDeserializer serializer,
                IDataProtectionProvider dataProtectionProvider,
                CircuitFactory circuitFactory,
                CircuitIdFactory circuitIdFactory,
                CircuitRegistry circuitRegistry,
                ILogger<ComponentHub> logger) : base(
                serializer,
                dataProtectionProvider,
                circuitFactory,
                circuitIdFactory,
                circuitRegistry,
                logger)
            { }

            public override CircuitHandle GetCircuitHandle()
            {
                return null;
            }

            public override CircuitHost GetCircuit()
            {
                if (circuitSet)
                {
                    var serviceScope = new Mock<IServiceScope>();
                    var circuitHost = TestCircuitHost.Create(
                    serviceScope: serviceScope.Object);
                    return circuitHost;
                }
                return null;
            }

            public override void SetCircuit(CircuitHost circuitHost)
            {
                circuitSet = true;
                return;
            }

            public override (bool, List<ComponentDescriptor>) DeserializeComponentDescriptor(string serializedComponentRecords)
            {
                List<ComponentDescriptor> descriptors = default;
                return (true, descriptors);
            }
        }

        private class TestCircuitFactory : CircuitFactory
        {
            public TestCircuitFactory(
            IServiceScopeFactory scopeFactory,
            ILoggerFactory loggerFactory,
            CircuitIdFactory circuitIdFactory,
            IOptions<CircuitOptions> options) : base(
            scopeFactory,
            loggerFactory,
            circuitIdFactory,
            options)
            { }

            // We override the default `CreateCircuitHostAsync` so we can mock around
            // the work of initializing a host and all its service dependencies.
            public override async ValueTask<CircuitHost> CreateCircuitHostAsync(
                IReadOnlyList<ComponentDescriptor> components,
                CircuitClientProxy client,
                string baseUri,
                string uri,
                ClaimsPrincipal user,
                IComponentApplicationStateStore store)
            {
                await Task.Delay(0);
                var serviceScope = new Mock<IServiceScope>();
                var circuitHost = TestCircuitHost.Create(serviceScope: serviceScope.Object);
                return circuitHost;
            }
        }
    }
}
