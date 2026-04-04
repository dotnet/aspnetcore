// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class PersistentComponentStateTest
{
    [Fact]
    public void InitializeExistingState_SetupsState()
    {
        // Arrange
        var applicationState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        var existingState = new Dictionary<string, byte[]>
        {
            ["MyState"] = JsonSerializer.SerializeToUtf8Bytes(new byte[] { 1, 2, 3, 4 })
        };

        // Act
        applicationState.InitializeExistingState(existingState, RestoreContext.InitialValue);

        // Assert
        Assert.True(applicationState.TryTakeFromJson<byte[]>("MyState", out var existing));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, existing);
    }

    [Fact]
    public void InitializeExistingState_ThrowsIfAlreadyInitialized()
    {
        // Arrange
        var applicationState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        var existingState = new Dictionary<string, byte[]>
        {
            ["MyState"] = new byte[] { 1, 2, 3, 4 }
        };

        applicationState.InitializeExistingState(existingState, RestoreContext.InitialValue);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => applicationState.InitializeExistingState(existingState, null));
    }

    [Fact]
    public void RegisterOnPersisting_ThrowsIfCalledDuringOnPersisting()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var applicationState = new PersistentComponentState(currentState, [], [])
        {
            PersistingState = true
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => applicationState.RegisterOnPersisting(() => Task.CompletedTask));
    }

    [Fact]
    public void TryRetrieveState_ReturnsStateWhenItExists()
    {
        // Arrange
        var applicationState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        var existingState = new Dictionary<string, byte[]>
        {
            ["MyState"] = JsonSerializer.SerializeToUtf8Bytes(new byte[] { 1, 2, 3, 4 })
        };

        // Act
        applicationState.InitializeExistingState(existingState, RestoreContext.InitialValue);

        // Assert
        Assert.True(applicationState.TryTakeFromJson<byte[]>("MyState", out var existing));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, existing);
        Assert.False(applicationState.TryTakeFromJson<byte[]>("MyState", out var gone));
    }

    [Fact]
    public void PersistState_SavesDataToTheStoreAsync()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var applicationState = new PersistentComponentState(currentState, [], [])
        {
            PersistingState = true
        };
        var myState = new byte[] { 1, 2, 3, 4 };

        // Act
        applicationState.PersistAsJson("MyState", myState);

        // Assert
        Assert.True(currentState.TryGetValue("MyState", out var stored));
        Assert.Equal(myState, JsonSerializer.Deserialize<byte[]>(stored));
    }

    [Fact]
    public void PersistState_ThrowsForDuplicateKeys()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var applicationState = new PersistentComponentState(currentState, [], [])
        {
            PersistingState = true
        };
        var myState = new byte[] { 1, 2, 3, 4 };

        applicationState.PersistAsJson("MyState", myState);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => applicationState.PersistAsJson("MyState", myState));
    }

    [Fact]
    public void PersistAsJson_SerializesTheDataToJsonAsync()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var applicationState = new PersistentComponentState(currentState, [], [])
        {
            PersistingState = true
        };
        var myState = new byte[] { 1, 2, 3, 4 };

        // Act
        applicationState.PersistAsJson("MyState", myState);

        // Assert
        Assert.True(currentState.TryGetValue("MyState", out var stored));
        Assert.Equal(myState, JsonSerializer.Deserialize<byte[]>(stored));
    }

    [Fact]
    public void PersistAsJson_NullValueAsync()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var applicationState = new PersistentComponentState(currentState, [], [])
        {
            PersistingState = true
        };

        // Act
        applicationState.PersistAsJson<byte[]>("MyState", null);

        // Assert
        Assert.True(currentState.TryGetValue("MyState", out var stored));
        Assert.Null(JsonSerializer.Deserialize<byte[]>(stored));
    }

    [Fact]
    public void TryRetrieveFromJson_DeserializesTheDataFromJson()
    {
        // Arrange
        var myState = new byte[] { 1, 2, 3, 4 };
        var serialized = JsonSerializer.SerializeToUtf8Bytes(myState);
        var existingState = new Dictionary<string, byte[]>() { ["MyState"] = serialized };
        var applicationState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);

        applicationState.InitializeExistingState(existingState, RestoreContext.InitialValue);

        // Act
        Assert.True(applicationState.TryTakeFromJson<byte[]>("MyState", out var stored));

        // Assert
        Assert.Equal(myState, stored);
        Assert.False(applicationState.TryTakeFromJson<byte[]>("MyState", out _));
    }

    [Fact]
    public void TryRetrieveFromJson_NullValue()
    {
        // Arrange
        var serialized = JsonSerializer.SerializeToUtf8Bytes<byte[]>(null);
        var existingState = new Dictionary<string, byte[]>() { ["MyState"] = serialized };
        var applicationState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);

        applicationState.InitializeExistingState(existingState, RestoreContext.InitialValue);

        // Act
        Assert.True(applicationState.TryTakeFromJson<byte[]>("MyState", out var stored));

        // Assert
        Assert.Null(stored);
        Assert.False(applicationState.TryTakeFromJson<byte[]>("MyState", out _));
    }

    [Fact]
    public void RegisterOnRestoring_InvokesCallbackWhenShouldRestoreMatches()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var callbacks = new List<RestoreComponentStateRegistration>();
        var applicationState = new PersistentComponentState(currentState, [], callbacks);
        applicationState.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);

        var callbackInvoked = false;
        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.Default, AllowUpdates = false };

        // Act
        var subscription = applicationState.RegisterOnRestoring(() => { callbackInvoked = true; }, options);

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void RegisterOnRestoring_DoesNotInvokeCallbackWhenShouldRestoreDoesNotMatch()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var callbacks = new List<RestoreComponentStateRegistration>();
        var applicationState = new PersistentComponentState(currentState, [], callbacks);
        applicationState.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);

        var callbackInvoked = false;
        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.SkipInitialValue, AllowUpdates = false };

        // Act
        var subscription = applicationState.RegisterOnRestoring(() => { callbackInvoked = true; }, options);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void RegisterOnRestoring_ReturnsDefaultSubscriptionWhenNotAllowingUpdates()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var callbacks = new List<RestoreComponentStateRegistration>();
        var applicationState = new PersistentComponentState(currentState, [], callbacks);
        applicationState.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);

        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.Default, AllowUpdates = false };

        // Act
        var subscription = applicationState.RegisterOnRestoring(() => { }, options);

        // Assert
        Assert.Equal(default, subscription);
        Assert.Empty(callbacks);
    }

    [Fact]
    public void RegisterOnRestoring_ReturnsRestoringSubscriptionWhenAllowsUpdates()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var callbacks = new List<RestoreComponentStateRegistration>();
        var applicationState = new PersistentComponentState(currentState, [], callbacks);
        applicationState.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);

        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.Default, AllowUpdates = true };

        // Act
        var subscription = applicationState.RegisterOnRestoring(() => { }, options);

        // Assert
        Assert.NotEqual(default, subscription);
        Assert.Single(callbacks);
    }

    [Fact]
    public void RegisterOnRestoring_SubscriptionCanBeDisposed()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var callbacks = new List<RestoreComponentStateRegistration>();
        var applicationState = new PersistentComponentState(currentState, [], callbacks);
        applicationState.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);

        var options = new RestoreOptions { RestoreBehavior = RestoreBehavior.Default, AllowUpdates = true };
        var subscription = applicationState.RegisterOnRestoring(() => { }, options);

        Assert.Single(callbacks);

        // Act
        subscription.Dispose();

        // Assert
        Assert.Empty(callbacks);
    }
}
