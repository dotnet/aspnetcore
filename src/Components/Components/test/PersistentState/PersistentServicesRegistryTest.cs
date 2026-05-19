// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.PersistentState;

public class PersistentServicesRegistryTest
{
    [Fact]
    public async Task PersistStateAsync_PersistsServiceProperties()
    {
        // Arrange
        var state = "myState";
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestService>()
            .AddPersistentService<TestService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var testService = scope.GetService<TestService>();
        testService.State = state;

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        var registry = persistenceManager.ServicesRegistry;

        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());
        var componentState = new PersistentComponentState(testStore.State, [], []);

        var secondScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var secondManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            secondScope);

        await secondManager.RestoreStateAsync(new TestStore(testStore.State));

        // Assert
        var service = secondScope.GetRequiredService<TestService>();
        Assert.Equal(state, service.State);
    }

    [Fact]
    public async Task PersistStateAsync_PersistsBaseServiceProperties()
    {
        // Arrange
        var state = "myState";
        var componentRenderMode = new TestRenderMode();
        var serviceProviderOne = new ServiceCollection()
            .AddScoped<BaseService, DerivedOne>()
            .AddPersistentService<BaseService>(componentRenderMode)
            .BuildServiceProvider();

        var serviceProviderTwo = new ServiceCollection()
            .AddScoped<BaseService, DerivedTwo>()
            .AddPersistentService<BaseService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProviderOne.CreateAsyncScope().ServiceProvider;
        var derivedOne = scope.GetService<BaseService>() as DerivedOne;
        derivedOne.State = state;

        var persistenceManagerOne = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManagerOne.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManagerOne.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        await persistenceManagerOne.PersistStateAsync(testStore, new TestRenderer());

        var scopeTwo = serviceProviderTwo.CreateAsyncScope().ServiceProvider;
        var persistenceManagerTwo = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scopeTwo);

        await persistenceManagerTwo.RestoreStateAsync(new TestStore(testStore.State));

        // Assert
        var derivedTwo = scopeTwo.GetRequiredService<BaseService>() as DerivedTwo;
        Assert.Equal(state, derivedTwo.State);
    }

    [Fact]
    public async Task PersistStateAsync_PersistsBaseClassPropertiesInDerivedInstance()
    {
        // Arrange
        var state = "baseState";
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<BaseServiceWithProperty, DerivedService>()
            .AddPersistentService<BaseServiceWithProperty>(componentRenderMode)
            .BuildServiceProvider();

        var derivedService = serviceProvider.GetService<BaseServiceWithProperty>() as DerivedService;
        derivedService.State = state;

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            serviceProvider.CreateAsyncScope().ServiceProvider);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        var secondManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            serviceProvider.CreateAsyncScope().ServiceProvider);

        await secondManager.RestoreStateAsync(new TestStore(testStore.State));

        // Assert
        var restoredService = serviceProvider.GetRequiredService<BaseServiceWithProperty>() as DerivedService;
        Assert.Equal(state, restoredService.State);
    }

    [Fact]
    public async Task PersistStateAsync_DoesNotPersistNullServiceProperties()
    {
        // Arrange
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestService>()
            .AddPersistentService<TestService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var testService = scope.GetService<TestService>();
        testService.State = null;

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());
        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        // Act
        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        // Assert
        var kvp = Assert.Single(testStore.State);
        Assert.Equal(typeof(PersistentServicesRegistry).FullName, kvp.Key);
    }

    [Fact]
    public async Task PersistStateAsync_DoesNotThrowIfServiceNotResolvedDuringRestore()
    {
        // Arrange
        var state = "myState";
        var componentRenderMode = new TestRenderMode();
        var serviceProviderOne = new ServiceCollection()
            .AddScoped<BaseService, DerivedOne>()
            .AddPersistentService<BaseService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProviderOne.CreateAsyncScope().ServiceProvider;
        var derivedOne = scope.GetService<BaseService>() as DerivedOne;
        derivedOne.State = state;

        var persistenceManagerOne = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManagerOne.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManagerOne.PersistStateAsync(testStore, new TestRenderer());

        var serviceProviderTwo = new ServiceCollection()
            .BuildServiceProvider();

        var scopeTwo = serviceProviderTwo.CreateAsyncScope().ServiceProvider;
        var persistenceManagerTwo = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scopeTwo);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => persistenceManagerTwo.RestoreStateAsync(new TestStore(testStore.State)));
        Assert.Null(exception);
    }

    [Fact]
    public async Task PersistStateAsync_RestoresStateForPersistedRegistrations()
    {
        // Arrange
        var state = "myState";
        var componentRenderMode = new TestRenderMode();
        var serviceProviderOne = new ServiceCollection()
            .AddScoped<BaseService, DerivedOne>()
            .AddPersistentService<BaseService>(componentRenderMode)
            .BuildServiceProvider();

        var serviceProviderTwo = new ServiceCollection()
            .AddScoped<BaseService, DerivedTwo>()
            .BuildServiceProvider();

        var scope = serviceProviderOne.CreateAsyncScope().ServiceProvider;
        var derivedOne = scope.GetService<BaseService>() as DerivedOne;
        derivedOne.State = state;

        var persistenceManagerOne = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManagerOne.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManagerOne.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        await persistenceManagerOne.PersistStateAsync(testStore, new TestRenderer());

        var scopeTwo = serviceProviderTwo.CreateAsyncScope().ServiceProvider;
        var persistenceManagerTwo = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scopeTwo);

        await persistenceManagerTwo.RestoreStateAsync(new TestStore(testStore.State), RestoreContext.InitialValue);

        // Assert
        var derivedTwo = scopeTwo.GetRequiredService<BaseService>() as DerivedTwo;
        Assert.Equal(state, derivedTwo.State);
    }

    [Fact]
    public async Task PersistStateAsync_DoesNotThrow_WhenTypeCantBeFoundForPersistedRegistrations()
    {
        // Arrange
        var componentRenderMode = new TestRenderMode();
        var serviceProviderOne = new ServiceCollection()
            .AddSingleton<IPersistentServiceRegistration>(new TestPersistentRegistration { Assembly = "FakeAssembly", FullTypeName = "FakeType" })
            .BuildServiceProvider();

        var serviceProviderTwo = new ServiceCollection()
            .BuildServiceProvider();

        var scope = serviceProviderOne.CreateAsyncScope().ServiceProvider;

        var persistenceManagerOne = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManagerOne.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManagerOne.PersistStateAsync(testStore, new TestRenderer());

        var scopeTwo = serviceProviderTwo.CreateAsyncScope().ServiceProvider;
        var persistenceManagerTwo = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scopeTwo);

        var exception = await Record.ExceptionAsync(async () => await persistenceManagerTwo.RestoreStateAsync(new TestStore(testStore.State)));
        Assert.Null(exception);
    }

    [Fact]
    public void ResolveRegistrations_RemovesDuplicateRegistrations()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddSingleton<RegisteredPersistentServiceRegistrationCollection>()
            .AddSingleton<IPersistentServiceRegistration>(new TestPersistentRegistration { Assembly = "Assembly1", FullTypeName = "Type1" })
            .AddSingleton<IPersistentServiceRegistration>(new TestPersistentRegistration { Assembly = "Assembly1", FullTypeName = "Type1" }) // Duplicate
            .AddSingleton<IPersistentServiceRegistration>(new TestPersistentRegistration { Assembly = "Assembly2", FullTypeName = "Type2" })
            .BuildServiceProvider();

        var registry = new PersistentServicesRegistry(serviceProvider);

        // Act
        var registrations = registry.Registrations;

        // Assert
        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, r => r.Assembly == "Assembly1" && r.FullTypeName == "Type1");
        Assert.Contains(registrations, r => r.Assembly == "Assembly2" && r.FullTypeName == "Type2");
    }

    private class TestStore : IPersistentComponentStateStore
    {
        public IDictionary<string, byte[]> State { get; set; }

        public TestStore(IDictionary<string, byte[]> initialState)
        {
            State = initialState;
        }

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return Task.FromResult(State);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            State = state.ToDictionary(k => k.Key, v => v.Value);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PersistStateAsync_PersistsMultipleServicesWithDifferentStates()
    {
        // Arrange
        var state1 = "state1";
        var state2 = "state2";
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestService>()
            .AddScoped<AnotherTestService>()
            .AddPersistentService<TestService>(componentRenderMode)
            .AddPersistentService<AnotherTestService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var testService = scope.GetService<TestService>();
        var anotherTestService = scope.GetService<AnotherTestService>();
        testService.State = state1;
        anotherTestService.State = state2;

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        var secondScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var secondManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            secondScope);

        await secondManager.RestoreStateAsync(new TestStore(testStore.State));

        // Assert
        var restoredTestService = secondScope.GetRequiredService<TestService>();
        var restoredAnotherTestService = secondScope.GetRequiredService<AnotherTestService>();
        Assert.Equal(state1, restoredTestService.State);
        Assert.Equal(state2, restoredAnotherTestService.State);
    }

    [Fact]
    public async Task PersistStateAsync_PersistsServiceWithComplexState()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "John Doe",
            Addresses =
            [
                new Address { Street = "123 Main St", ZipCode = "12345" },
                new Address { Street = "456 Elm St", ZipCode = "67890" }
            ]
        };
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<CustomerService>()
            .AddPersistentService<CustomerService>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var customerService = scope.GetService<CustomerService>();
        customerService.Customer = customer;

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());
        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);

        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        var secondScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var secondManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            secondScope);

        await secondManager.RestoreStateAsync(new TestStore(testStore.State));

        // Assert
        var restoredCustomerService = secondScope.GetRequiredService<CustomerService>();
        Assert.Equal(customer.Name, restoredCustomerService.Customer.Name);
        Assert.Equal(customer.Addresses.Count, restoredCustomerService.Customer.Addresses.Count);
        for (var i = 0; i < customer.Addresses.Count; i++)
        {
            Assert.Equal(customer.Addresses[i].Street, restoredCustomerService.Customer.Addresses[i].Street);
            Assert.Equal(customer.Addresses[i].ZipCode, restoredCustomerService.Customer.Addresses[i].ZipCode);
        }
    }

    [Fact]
    public async Task PersistStateAsync_RespectsSkipInitialValueBehavior()
    {
        // Arrange
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<ServiceWithSkipInitialValue>()
            .AddPersistentService<ServiceWithSkipInitialValue>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var service = scope.GetRequiredService<ServiceWithSkipInitialValue>();
        service.SkipInitialValueProperty = "TestValue";

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);
        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        // Act - Restore with InitialValue context
        var initialValueScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var initialValueManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            initialValueScope);

        await initialValueManager.RestoreStateAsync(new TestStore(testStore.State), RestoreContext.InitialValue);
        var restoredService = initialValueScope.GetRequiredService<ServiceWithSkipInitialValue>();

        // Assert - Property should be null because it was skipped during InitialValue restore
        Assert.Null(restoredService.SkipInitialValueProperty);
    }

    [Fact]
    public async Task PersistStateAsync_RespectsSkipLastSnapshotBehavior()
    {
        // Arrange
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<ServiceWithSkipLastSnapshot>()
            .AddPersistentService<ServiceWithSkipLastSnapshot>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var service = scope.GetRequiredService<ServiceWithSkipLastSnapshot>();
        service.SkipLastSnapshotProperty = "TestValue";

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var testStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);
        await persistenceManager.PersistStateAsync(testStore, new TestRenderer());

        // Act - Restore with LastSnapshot context
        var lastSnapshotScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var lastSnapshotManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            lastSnapshotScope);

        await lastSnapshotManager.RestoreStateAsync(new TestStore(testStore.State), RestoreContext.LastSnapshot);
        var restoredService = lastSnapshotScope.GetRequiredService<ServiceWithSkipLastSnapshot>();

        // Assert - Property should be null because it was skipped during LastSnapshot restore
        Assert.Null(restoredService.SkipLastSnapshotProperty);
    }

    [Fact]
    public async Task PersistStateAsync_RespectsAllowUpdatesBehavior()
    {
        // Arrange
        var componentRenderMode = new TestRenderMode();
        var serviceProvider = new ServiceCollection()
            .AddScoped<ServiceWithAllowUpdates>()
            .AddPersistentService<ServiceWithAllowUpdates>(componentRenderMode)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var service = scope.GetRequiredService<ServiceWithAllowUpdates>();
        service.AllowUpdatesProperty = "InitialValue";

        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            scope);
        persistenceManager.SetPlatformRenderMode(componentRenderMode);
        var initialStore = new TestStore(new Dictionary<string, byte[]>());

        await persistenceManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);
        await persistenceManager.PersistStateAsync(initialStore, new TestRenderer());

        // Create updated state
        var updatedScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var updatedService = updatedScope.GetRequiredService<ServiceWithAllowUpdates>();
        updatedService.AllowUpdatesProperty = "UpdatedValue";

        var updatedManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            updatedScope);
        var updatedStore = new TestStore(new Dictionary<string, byte[]>());

        await updatedManager.RestoreStateAsync(new TestStore(new Dictionary<string, byte[]>()), RestoreContext.InitialValue);
        await updatedManager.PersistStateAsync(updatedStore, new TestRenderer());

        // Act - First restore with InitialValue, then update with ValueUpdate context
        var targetScope = serviceProvider.CreateAsyncScope().ServiceProvider;
        var targetManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            targetScope);

        await targetManager.RestoreStateAsync(new TestStore(initialStore.State), RestoreContext.InitialValue);
        var restoredService = targetScope.GetRequiredService<ServiceWithAllowUpdates>();
        Assert.Equal("InitialValue", restoredService.AllowUpdatesProperty);

        // Update with ValueUpdate context
        await targetManager.RestoreStateAsync(new TestStore(updatedStore.State), RestoreContext.ValueUpdate);

        // Assert - Property should be updated because AllowUpdates is true
        Assert.Equal("UpdatedValue", restoredService.AllowUpdatesProperty);
    }

    private class ServiceWithSkipInitialValue
    {
        [PersistentState(RestoreBehavior = RestoreBehavior.SkipInitialValue)]
        public string SkipInitialValueProperty { get; set; }
    }

    private class ServiceWithSkipLastSnapshot
    {
        [PersistentState(RestoreBehavior = RestoreBehavior.SkipLastSnapshot)]
        public string SkipLastSnapshotProperty { get; set; }
    }

    private class ServiceWithAllowUpdates
    {
        [PersistentState(AllowUpdates = true)]
        public string AllowUpdatesProperty { get; set; }
    }

    private class AnotherTestService
    {
        [PersistentState]
        public string State { get; set; }
    }

    private class CustomerService
    {
        [PersistentState]
        public Customer Customer { get; set; }
    }

    private class Customer
    {
        public string Name { get; set; }
        public List<Address> Addresses { get; set; }
    }

    private class Address
    {
        public string Street { get; set; }
        public string ZipCode { get; set; }
    }

    private class TestRenderMode : IComponentRenderMode
    {
    }

    private class TestService
    {
        [PersistentState]
        public string State { get; set; }
    }

    private class BaseTestService
    {
        public string BaseState { get; }

        public BaseTestService(string baseState)
        {
            BaseState = baseState;
        }
    }

    private class DerivedTestService : BaseTestService
    {
        public string DerivedState { get; }

        public DerivedTestService(string baseState, string derivedState)
            : base(baseState)
        {
            DerivedState = derivedState;
        }
    }

    private class TestRenderer : Renderer
    {
        public TestRenderer() : base(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        private readonly Dispatcher _dispatcher = Dispatcher.CreateDefault();

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }
    }

    private class BaseService
    {
    }

    private class DerivedOne : BaseService
    {
        [PersistentState]
        public string State { get; set; }
    }

    private class DerivedTwo : BaseService
    {
        [PersistentState]
        public string State { get; set; }
    }

    private class BaseServiceWithProperty
    {
        [PersistentState]
        public string State { get; set; }
    }

    private class DerivedService : BaseServiceWithProperty
    {
    }

    private class TestPersistentRegistration : IPersistentServiceRegistration
    {
        public string Assembly { get; set; }
        public string FullTypeName { get; set; }

        public IComponentRenderMode GetRenderModeOrDefault() => null;
    }
}

static file class ComponentStatePersistenceManagerExtensions
{
    public static IServiceCollection AddPersistentService<TPersistentService>(this IServiceCollection services, IComponentRenderMode renderMode)
    {
        RegisterPersistentComponentStateServiceCollectionExtensions.AddPersistentServiceRegistration<TPersistentService>(
            services,
            renderMode);
        return services;
    }
}
