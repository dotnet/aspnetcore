// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class IPersistentComponentStateSerializerTests
{
    [Fact]
    public void PersistAsync_CanUseCustomSerializer()
    {
        // Arrange
        var currentState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(currentState, []);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var stateValueProvider = new PersistentStateValueProvider(state, serviceProvider);
        var customSerializer = new TestStringSerializer();
        var testValue = "Hello, World!";

        state.PersistingState = true;

        // Act
        stateValueProvider.PersistAsync("test-key", testValue, customSerializer);

        // Assert
        state.PersistingState = false;
        
        // Simulate the state transfer that happens between persist and restore phases
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        newState.InitializeExistingState(currentState);
        var newStateValueProvider = new PersistentStateValueProvider(newState, serviceProvider);
        
        Assert.True(newStateValueProvider.TryTake("test-key", customSerializer, out var retrievedValue));
        Assert.Equal(testValue, retrievedValue);
    }

    [Fact]
    public void TryTake_CanUseCustomSerializer()
    {
        // Arrange
        var customData = "Custom Data";
        var customBytes = Encoding.UTF8.GetBytes(customData);
        var existingState = new Dictionary<string, byte[]> { { "test-key", customBytes } };
        
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        state.InitializeExistingState(existingState);
        
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var stateValueProvider = new PersistentStateValueProvider(state, serviceProvider);
        var customSerializer = new TestStringSerializer();

        // Act
        var success = stateValueProvider.TryTake("test-key", customSerializer, out var retrievedValue);

        // Assert
        Assert.True(success);
        Assert.Equal(customData, retrievedValue);
    }

    private class TestStringSerializer : PersistentComponentStateSerializer<string>
    {
        public override void Persist(string value, IBufferWriter<byte> writer)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes);
        }

        public override string Restore(ReadOnlySequence<byte> data)
        {
            var bytes = data.ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
    }
}