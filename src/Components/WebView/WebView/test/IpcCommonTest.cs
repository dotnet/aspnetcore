// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView;

public class IpcCommonTest
{
    // The options IPC used before it switched to source-generated serialization. Messages must stay byte-for-byte
    // identical, since blazor.webview.js and existing hosts depend on the format.
    private static readonly JsonSerializerOptions ReflectionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
    };

    public static TheoryData<object[]> OutgoingMessageArguments => new()
    {
        // RenderBatch
        new object[] { 1L, "AQID" },
        // Navigate
        new object[] { "https://0.0.0.1/page", new NavigationOptions() },
        new object[] { "https://0.0.0.1/page", new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true, HistoryEntryState = "state" } },
        // Refresh / SetHasLocationChangingListeners
        new object[] { true },
        new object[] { false },
        // AttachToDocument
        new object[] { 3, "#app" },
        // BeginInvokeJS
        new object[] { 7L, "console.log", "[\"hello\"]", JSCallResultType.Default, 0L, JSCallType.FunctionCall },
        new object[] { long.MaxValue, "Blazor._internal.x", null, JSCallResultType.JSVoidResult, 42L, JSCallType.GetValue },
        // EndInvokeDotNet
        new object[] { "1", true, "[1,true,{\"result\":1}]" },
        // SendByteArrayToJS
        new object[] { 5, new byte[] { 0, 1, 2, 255 } },
        // EndLocationChanging
        new object[] { 2, false },
        // NotifyUnhandledException, including characters that must be escaped identically
        new object[] { "<script>alert('x') & \"quoted\"</script>", "at Foo() in C:\\path\\file.cs:line 1\n\tüñí©ødé 😀" },
        new object[] { "message", null },
    };

    [Theory]
    [MemberData(nameof(OutgoingMessageArguments))]
    public void Serialize_ProducesSameMessageAsReflectionBasedSerialization(object[] args)
    {
        // Act
        var message = IpcCommon.Serialize(IpcCommon.OutgoingMessageType.BeginInvokeJS, args);

        // Assert
        var expected = "__bwv:" + JsonSerializer.Serialize(args.Prepend(nameof(IpcCommon.OutgoingMessageType.BeginInvokeJS)), ReflectionOptions);
        Assert.Equal(expected, message);
    }

    [Fact]
    public void Serialize_RoundTripsThroughTryDeserialize()
    {
        // Arrange
        var message = IpcCommon.Serialize(
            IpcCommon.OutgoingMessageType.Navigate,
            "https://0.0.0.1/page",
            new NavigationOptions { ForceLoad = true, HistoryEntryState = "state" });

        // Act
        var parsed = IpcCommon.TryDeserializeOutgoing(message, out var messageType, out var args);

        // Assert
        Assert.True(parsed);
        Assert.Equal(IpcCommon.OutgoingMessageType.Navigate, messageType);
        Assert.Equal(2, args.Count);
        Assert.Equal("https://0.0.0.1/page", args[0].GetString());
        Assert.True(args[1].GetProperty("forceLoad").GetBoolean());
        Assert.False(args[1].GetProperty("replaceHistoryEntry").GetBoolean());
        Assert.Equal("state", args[1].GetProperty("historyEntryState").GetString());
    }

    [Fact]
    public void TryDeserializeIncoming_ParsesMessageTypeAndArguments()
    {
        // Act
        var parsed = IpcCommon.TryDeserializeIncoming(
            "__bwv:[\"OnLocationChanged\",\"https://0.0.0.1/next\",null,true]",
            out var messageType,
            out var args);

        // Assert
        Assert.True(parsed);
        Assert.Equal(IpcCommon.IncomingMessageType.OnLocationChanged, messageType);
        Assert.Equal(3, args.Count);
        Assert.Equal("https://0.0.0.1/next", args[0].GetString());
        Assert.Equal(JsonValueKind.Null, args[1].ValueKind);
        Assert.True(args[2].GetBoolean());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a blazor message")]
    [InlineData("[\"AttachPage\",\"https://0.0.0.1/\",\"https://0.0.0.1/\"]")]
    public void TryDeserializeIncoming_IgnoresMessagesWithoutPrefix(string message)
    {
        // Act
        var parsed = IpcCommon.TryDeserializeIncoming(message, out _, out var args);

        // Assert
        Assert.False(parsed);
        Assert.Equal(0, args.Count);
    }

    [Fact]
    public void Serialize_ThrowsForUnsupportedArgumentType()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => IpcCommon.Serialize(IpcCommon.OutgoingMessageType.Navigate, Guid.Empty));
        Assert.Contains(typeof(Guid).FullName, ex.Message);
        Assert.Contains(nameof(IpcJsonSerializerContext), ex.Message);
    }
}
