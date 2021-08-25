// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class ComponentApplicationStateTest
    {
        [Fact]
        public void InitializeExistingState_SetupsState()
        {
            // Arrange
            var applicationState = new PersistentComponentState(new Dictionary<string, PooledByteBufferWriter>(), new List<Func<Task>>());
            var existingState = new Dictionary<string, ReadOnlySequence<byte>>
            {
                ["MyState"] = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4 })
            };

            // Act
            applicationState.InitializeExistingState(existingState);

            // Assert
            Assert.True(applicationState.TryTake("MyState", out var existing));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, existing.ToArray());
        }

        [Fact]
        public void InitializeExistingState_ThrowsIfAlreadyInitialized()
        {
            // Arrange
            var applicationState = new PersistentComponentState(new Dictionary<string, PooledByteBufferWriter>(), new List<Func<Task>>());
            var existingState = new Dictionary<string, ReadOnlySequence<byte>>
            {
                ["MyState"] = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4 })
            };

            applicationState.InitializeExistingState(existingState);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => applicationState.InitializeExistingState(existingState));
        }

        [Fact]
        public void TryRetrieveState_ReturnsStateWhenItExists()
        {
            // Arrange
            var applicationState = new PersistentComponentState(new Dictionary<string, PooledByteBufferWriter>(), new List<Func<Task>>());
            var existingState = new Dictionary<string, ReadOnlySequence<byte>>
            {
                ["MyState"] = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4 })
            };

            // Act
            applicationState.InitializeExistingState(existingState);

            // Assert
            Assert.True(applicationState.TryTake("MyState", out var existing));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, existing.ToArray());
            Assert.False(applicationState.TryTake("MyState", out var gone));
        }

        [Fact]
        public void PersistState_SavesDataToTheStoreAsync()
        {
            // Arrange
            var currentState = new Dictionary<string, PooledByteBufferWriter>();
            var applicationState = new PersistentComponentState(currentState, new List<Func<Task>>());
            applicationState.PersistingState = true;
            var myState = new byte[] { 1, 2, 3, 4 };

            // Act
            applicationState.Persist("MyState", writer => writer.Write(myState));

            // Assert
            Assert.True(currentState.TryGetValue("MyState", out var stored));
            Assert.Equal(myState, stored.WrittenMemory.Span.ToArray());
        }

        [Fact]
        public void PersistState_ThrowsForDuplicateKeys()
        {
            // Arrange
            var currentState = new Dictionary<string, PooledByteBufferWriter>();
            var applicationState = new PersistentComponentState(currentState, new List<Func<Task>>());
            applicationState.PersistingState = true;
            var myState = new byte[] { 1, 2, 3, 4 };

            applicationState.Persist("MyState", writer => writer.Write(myState));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => applicationState.Persist("MyState", writer => writer.Write(myState)));
        }

        [Fact]
        public void PersistAsJson_SerializesTheDataToJsonAsync()
        {
            // Arrange
            var currentState = new Dictionary<string, PooledByteBufferWriter>();
            var applicationState = new PersistentComponentState(currentState, new List<Func<Task>>());
            applicationState.PersistingState = true;
            var myState = new byte[] { 1, 2, 3, 4 };

            // Act
            applicationState.PersistAsJson("MyState", myState);

            // Assert
            Assert.True(currentState.TryGetValue("MyState", out var stored));
            Assert.Equal(myState, JsonSerializer.Deserialize<byte[]>(stored.WrittenMemory.Span));
        }

        [Fact]
        public void PersistAsJson_NullValueAsync()
        {
            // Arrange
            var currentState = new Dictionary<string, PooledByteBufferWriter>();
            var applicationState = new PersistentComponentState(currentState, new List<Func<Task>>());
            applicationState.PersistingState = true;

            // Act
            applicationState.PersistAsJson<byte []>("MyState", null);

            // Assert
            Assert.True(currentState.TryGetValue("MyState", out var stored));
            Assert.Null(JsonSerializer.Deserialize<byte[]>(stored.WrittenMemory.Span));
        }

        [Fact]
        public void TryRetrieveFromJson_DeserializesTheDataFromJson()
        {
            // Arrange
            var myState = new byte[] { 1, 2, 3, 4 };
            var serialized = JsonSerializer.SerializeToUtf8Bytes(myState);
            var existingState = new Dictionary<string, ReadOnlySequence<byte>>() { ["MyState"] = new ReadOnlySequence<byte>(serialized) };
            var applicationState = new PersistentComponentState(new Dictionary<string, PooledByteBufferWriter>(), new List<Func<Task>>());

            applicationState.InitializeExistingState(existingState);

            // Act
            Assert.True(applicationState.TryTakeFromJson<byte []>("MyState", out var stored));

            // Assert
            Assert.Equal(myState, stored);
            Assert.False(applicationState.TryTakeFromJson<byte[]>("MyState", out _));
        }

        [Fact]
        public void TryRetrieveFromJson_NullValue()
        {
            // Arrange
            var serialized = JsonSerializer.SerializeToUtf8Bytes<byte []>(null);
            var existingState = new Dictionary<string, ReadOnlySequence<byte>>() { ["MyState"] = new ReadOnlySequence<byte>(serialized) };
            var applicationState = new PersistentComponentState(new Dictionary<string, PooledByteBufferWriter>(), new List<Func<Task>>());

            applicationState.InitializeExistingState(existingState);

            // Act
            Assert.True(applicationState.TryTakeFromJson<byte[]>("MyState", out var stored));

            // Assert
            Assert.Null(stored);
            Assert.False(applicationState.TryTakeFromJson<byte[]>("MyState", out _));
        }
    }
}
