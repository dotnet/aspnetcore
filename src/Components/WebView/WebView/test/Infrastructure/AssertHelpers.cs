// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.WebView;

public class AssertHelpers
{
    internal static void IsAttachWebRendererInteropMessage(string message)
    {
        Assert.True(IpcCommon.TryDeserializeOutgoing(message, out var messageType, out var args));
        Assert.Equal(IpcCommon.OutgoingMessageType.BeginInvokeJS, messageType);
        Assert.Equal(5, args.Count);
        Assert.Equal("Blazor._internal.attachWebRendererInterop", args[1].GetString());
    }

    internal static void IsAttachToDocumentMessage(string message, int componentId, string selector)
    {
        Assert.True(IpcCommon.TryDeserializeOutgoing(message, out var messageType, out var args));
        Assert.Equal(IpcCommon.OutgoingMessageType.AttachToDocument, messageType);
        Assert.Equal(2, args.Count);
        Assert.Equal(componentId, args[0].GetInt32());
        Assert.Equal(selector, args[1].GetString());
    }

    internal static RenderBatch IsRenderBatch(string message)
    {
        Assert.True(IpcCommon.TryDeserializeOutgoing(message, out var messageType, out var args));
        Assert.Equal(IpcCommon.OutgoingMessageType.RenderBatch, messageType);
        Assert.Equal(2, args.Count);
        Assert.Equal(1, args[0].GetInt64()); // Batch ID

        // At least validate we can base64 decode the batch data
        var _ = Convert.FromBase64String(args[1].GetString());
        // TODO: Produce the render batch if we want to grab info from it.
        return default;
    }
}
