// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components;

public class ComponentStatePersistenceManagerFilteringTests
{
    [Fact]
    public async Task RestoreStateAsync_WithNoFilter_ShouldRestoreForAnyScenario()
    {
        // Arrange
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("test-key", "test-value");
        
        var callbackExecuted = false;
        var restoredValue = string.Empty;
        
        // Register a restoration callback with NO filter (registration.Filter == null)
        manager.State.RegisterOnRestoring(filter: null, () =>
        {
            callbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key", out var value))
            {
                restoredValue = value;
            }
        });

        // Act & Assert for Prerendering scenario
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Prerendering);
        
        Assert.True(callbackExecuted, "Callback should execute for prerendering when no filter is applied");
        Assert.Equal("test-value", restoredValue);

        // Reset store for next test
        store.SetState("test-key", "test-value");
        
        // Act & Assert for Reconnection scenario  
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Reconnection);
        
        Assert.True(callbackExecuted, "Callback should execute for reconnection when no filter is applied");
        Assert.Equal("test-value", restoredValue);
    }

    [Fact]
    public async Task RestoreStateAsync_WithReconnectionFilter_ShouldOnlyRestoreForReconnection()
    {
        // Arrange
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("test-key", "test-value");
        
        var callbackExecuted = false;
        var restoredValue = string.Empty;
        
        // Register a restoration callback with Reconnection filter
        var reconnectionFilter = WebPersistenceFilter.Reconnection;
        manager.State.RegisterOnRestoring(reconnectionFilter, () =>
        {
            callbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key", out var value))
            {
                restoredValue = value;
            }
        });

        // Act & Assert for Prerendering scenario (should NOT execute)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Prerendering);
        
        Assert.False(callbackExecuted, "Callback should NOT execute for prerendering when reconnection filter is applied");
        Assert.Equal(string.Empty, restoredValue);

        // Reset store for next test
        store.SetState("test-key", "test-value");
        
        // Act & Assert for Reconnection scenario (should execute)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Reconnection);
        
        Assert.True(callbackExecuted, "Callback should execute for reconnection when reconnection filter is applied");
        Assert.Equal("test-value", restoredValue);
    }

    [Fact]
    public async Task RestoreStateAsync_WithPrerenderingFilter_ShouldOnlyRestoreForPrerendering()
    {
        // Arrange
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("test-key", "test-value");
        
        var callbackExecuted = false;
        var restoredValue = string.Empty;
        
        // Register a restoration callback with Prerendering filter
        var prerenderingFilter = WebPersistenceFilter.Prerendering;
        manager.State.RegisterOnRestoring(prerenderingFilter, () =>
        {
            callbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key", out var value))
            {
                restoredValue = value;
            }
        });

        // Act & Assert for Prerendering scenario (should execute)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Prerendering);
        
        Assert.True(callbackExecuted, "Callback should execute for prerendering when prerendering filter is applied");
        Assert.Equal("test-value", restoredValue);

        // Reset store for next test
        store.SetState("test-key", "test-value");
        
        // Act & Assert for Reconnection scenario (should NOT execute)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Reconnection);
        
        Assert.False(callbackExecuted, "Callback should NOT execute for reconnection when prerendering filter is applied");
        Assert.Equal(string.Empty, restoredValue);
    }

    [Fact]
    public async Task RestoreStateAsync_WithDisabledReconnectionFilter_ShouldNotRestoreForReconnection()
    {
        // Arrange
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("test-key", "test-value");
        
        var callbackExecuted = false;
        var restoredValue = string.Empty;
        
        // Register a restoration callback with DISABLED Reconnection filter ([RestoreStateOnReconnection(false)])
        var disabledReconnectionFilter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: false);
        manager.State.RegisterOnRestoring(disabledReconnectionFilter, () =>
        {
            callbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key", out var value))
            {
                restoredValue = value;
            }
        });

        // Act & Assert for Prerendering scenario (filter doesn't support, so should execute)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Prerendering);
        
        Assert.True(callbackExecuted, "Callback should execute for prerendering when disabled reconnection filter is applied (filter doesn't support prerendering)");
        Assert.Equal("test-value", restoredValue);

        // Reset store for next test
        store.SetState("test-key", "test-value");
        
        // Act & Assert for Reconnection scenario (should NOT execute because filter is disabled)
        callbackExecuted = false;
        restoredValue = string.Empty;
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Reconnection);
        
        Assert.False(callbackExecuted, "Callback should NOT execute for reconnection when disabled reconnection filter is applied");
        Assert.Equal(string.Empty, restoredValue);
    }

    [Fact]
    public async Task RestoreStateAsync_WithNullScenario_ShouldAlwaysRestore()
    {
        // Arrange
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("test-key1", "value-no-filter");
        store.SetState("test-key2", "value-with-filter");
        
        var noFilterCallbackExecuted = false;
        var withFilterCallbackExecuted = false;
        var noFilterRestoredValue = string.Empty;
        var withFilterRestoredValue = string.Empty;
        
        // Register restoration callbacks - one with no filter, one with reconnection filter
        manager.State.RegisterOnRestoring(filter: null, () =>
        {
            noFilterCallbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key1", out var value))
            {
                noFilterRestoredValue = value;
            }
        });
        
        manager.State.RegisterOnRestoring(WebPersistenceFilter.Reconnection, () =>
        {
            withFilterCallbackExecuted = true;
            if (manager.State.TryTakeFromJson<string>("test-key2", out var value))
            {
                withFilterRestoredValue = value;
            }
        });

        // Act - Restore with null scenario (should execute ALL callbacks regardless of filters)
        await manager.RestoreStateAsync(store, scenario: null);
        
        // Assert
        Assert.True(noFilterCallbackExecuted, "Callback with no filter should execute when scenario is null");
        Assert.True(withFilterCallbackExecuted, "Callback with filter should execute when scenario is null");
        Assert.Equal("value-no-filter", noFilterRestoredValue);
        Assert.Equal("value-with-filter", withFilterRestoredValue);
    }

    [Fact]
    public async Task RestoreStateAsync_MixedFilters_ShouldRestoreCorrectly()
    {
        // Arrange - This simulates a component with multiple properties with different filters
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var store = new TestPersistentComponentStateStore();
        store.SetState("no-filter-key", "always-restore");
        store.SetState("reconnection-key", "reconnection-only");
        store.SetState("prerendering-key", "prerendering-only");
        store.SetState("disabled-reconnection-key", "disabled-for-reconnection");
        
        var results = new Dictionary<string, string>();
        
        // Property with no filter - should restore for any scenario
        manager.State.RegisterOnRestoring(filter: null, () =>
        {
            if (manager.State.TryTakeFromJson<string>("no-filter-key", out var value))
            {
                results["no-filter"] = value;
            }
        });
        
        // Property with reconnection filter - should only restore for reconnection
        manager.State.RegisterOnRestoring(WebPersistenceFilter.Reconnection, () =>
        {
            if (manager.State.TryTakeFromJson<string>("reconnection-key", out var value))
            {
                results["reconnection"] = value;
            }
        });
        
        // Property with prerendering filter - should only restore for prerendering
        manager.State.RegisterOnRestoring(WebPersistenceFilter.Prerendering, () =>
        {
            if (manager.State.TryTakeFromJson<string>("prerendering-key", out var value))
            {
                results["prerendering"] = value;
            }
        });
        
        // Property with disabled reconnection filter - should not restore for reconnection
        var disabledReconnectionFilter = new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: false);
        manager.State.RegisterOnRestoring(disabledReconnectionFilter, () =>
        {
            if (manager.State.TryTakeFromJson<string>("disabled-reconnection-key", out var value))
            {
                results["disabled-reconnection"] = value;
            }
        });

        // Act & Assert for Prerendering scenario
        results.Clear();
        store.ResetState();
        store.SetState("no-filter-key", "always-restore");
        store.SetState("reconnection-key", "reconnection-only");
        store.SetState("prerendering-key", "prerendering-only");
        store.SetState("disabled-reconnection-key", "disabled-for-reconnection");
        
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Prerendering);
        
        Assert.Equal("always-restore", results.GetValueOrDefault("no-filter"));
        Assert.False(results.ContainsKey("reconnection"));
        Assert.Equal("prerendering-only", results.GetValueOrDefault("prerendering"));
        Assert.Equal("disabled-for-reconnection", results.GetValueOrDefault("disabled-reconnection"));

        // Act & Assert for Reconnection scenario
        results.Clear();
        store.ResetState();
        store.SetState("no-filter-key", "always-restore");
        store.SetState("reconnection-key", "reconnection-only");
        store.SetState("prerendering-key", "prerendering-only");
        store.SetState("disabled-reconnection-key", "disabled-for-reconnection");
        
        await manager.RestoreStateAsync(store, WebPersistenceScenario.Reconnection);
        
        Assert.Equal("always-restore", results.GetValueOrDefault("no-filter"));
        Assert.Equal("reconnection-only", results.GetValueOrDefault("reconnection"));
        Assert.False(results.ContainsKey("prerendering"));
        Assert.False(results.ContainsKey("disabled-reconnection"));
    }

    private class TestPersistentComponentStateStore : IPersistentComponentStateStore
    {
        private Dictionary<string, byte[]> _state = new();

        public void SetState(string key, string value)
        {
            _state[key] = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value);
        }

        public void ResetState()
        {
            _state.Clear();
        }

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return Task.FromResult<IDictionary<string, byte[]>>(_state);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            _state = new Dictionary<string, byte[]>(state);
            return Task.CompletedTask;
        }

        public bool SupportsRenderMode(IComponentRenderMode renderMode) => true;
    }
}
