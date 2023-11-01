// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebView;

internal sealed class IpcCommon
{
    private const string _ipcMessagePrefix = "__bwv:";

    public static string Serialize(OutgoingMessageType messageType, params object[] args)
        => Serialize(messageType.ToString(), args);

    public static string Serialize(IncomingMessageType messageType, params object[] args)
        => Serialize(messageType.ToString(), args);

    public static bool TryDeserializeIncoming(string message, out IncomingMessageType messageType, out ArraySegment<JsonElement> args)
        => TryDeserialize(message, out messageType, out args);

    public static bool TryDeserializeOutgoing(string message, out OutgoingMessageType messageType, out ArraySegment<JsonElement> args)
        => TryDeserialize(message, out messageType, out args);

    private static string Serialize(string messageType, object[] args)
    {
        // We could come up with something a little more low-level here if we
        // wanted to avoid a couple of allocations
        var messageTypeAndArgs = args.Prepend(messageType);

        // Note we do NOT need the JSRuntime specific JsonSerializerOptions as the args needing special handling
        // (JS/DotNetObjectReference & Byte Arrays) have already been serialized earlier in the JSRuntime.
        // We run the serialization here to add the `messageType`.
        return $"{_ipcMessagePrefix}{JsonSerializer.Serialize(messageTypeAndArgs, JsonSerializerOptionsProvider.Options)}";
    }

    private static bool TryDeserialize<T>(string message, out T messageType, out ArraySegment<JsonElement> args)
    {
        // We don't want to get confused by unrelated messages that the developer is sending
        // over the same webview IPC channel, so ignore anything else
        if (message != null && message.StartsWith(_ipcMessagePrefix, StringComparison.Ordinal))
        {
            var messageAfterPrefix = message.AsSpan(_ipcMessagePrefix.Length);
            var parsed = (JsonElement[])JsonSerializer.Deserialize(messageAfterPrefix, typeof(JsonElement[]), JsonSerializerOptionsProvider.Options);
            messageType = (T)Enum.Parse(typeof(T), parsed[0].GetString());
            args = new ArraySegment<JsonElement>(parsed, 1, parsed.Length - 1);
            return true;
        }
        else
        {
            messageType = default;
            args = default;
            return false;
        }
    }

    public enum IncomingMessageType
    {
        AttachPage,
        BeginInvokeDotNet,
        EndInvokeJS,
        OnRenderCompleted,
        OnLocationChanged,
        ReceiveByteArrayFromJS,
        OnLocationChanging,
    }

    public enum OutgoingMessageType
    {
        RenderBatch,
        Navigate,
        AttachToDocument,
        EndInvokeDotNet,
        NotifyUnhandledException,
        BeginInvokeJS,
        SendByteArrayToJS,
        SetHasLocationChangingListeners,
        EndLocationChanging,
        Refresh,
    }
}
