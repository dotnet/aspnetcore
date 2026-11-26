// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView;

public class IpcCommonTests
{
    [Fact]
    public void PreservesOutgoingWireShapes()
    {
        (IpcCommon.OutgoingMessageType Kind, object[] Args, string Json)[] messages =
        [
            (IpcCommon.OutgoingMessageType.RenderBatch, [42L, "AQID"], """["RenderBatch",42,"AQID"]"""),
            (IpcCommon.OutgoingMessageType.Navigate, ["/next", new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true, HistoryEntryState = "s" }],
                """["Navigate","/next",{"forceLoad":true,"replaceHistoryEntry":true,"historyEntryState":"s","relativeToCurrentUri":false}]"""),
            (IpcCommon.OutgoingMessageType.AttachToDocument, [3, "#app"], """["AttachToDocument",3,"#app"]"""),
            (IpcCommon.OutgoingMessageType.EndInvokeDotNet, ["7", true, """{"value":42}"""], """["EndInvokeDotNet","7",true,"{\u0022value\u0022:42}"]"""),
            (IpcCommon.OutgoingMessageType.NotifyUnhandledException, ["error", "stack"], """["NotifyUnhandledException","error","stack"]"""),
            (IpcCommon.OutgoingMessageType.BeginInvokeJS, [7L, "echo", "[42]", JSCallResultType.Default, 0L, JSCallType.FunctionCall],
                """["BeginInvokeJS",7,"echo","[42]",0,0,1]"""),
            (IpcCommon.OutgoingMessageType.SendByteArrayToJS, [0, new byte[] { 1, 2, 3 }], """["SendByteArrayToJS",0,"AQID"]"""),
            (IpcCommon.OutgoingMessageType.SetHasLocationChangingListeners, [true], """["SetHasLocationChangingListeners",true]"""),
            (IpcCommon.OutgoingMessageType.EndLocationChanging, [4, false], """["EndLocationChanging",4,false]"""),
            (IpcCommon.OutgoingMessageType.Refresh, [true], """["Refresh",true]"""),
        ];

        foreach (var (kind, args, json) in messages)
        {
            var actual = IpcCommon.Serialize(kind, args);
            Assert.Equal("__bwv:" + json, actual);
            Assert.True(IpcCommon.TryDeserializeOutgoing(actual, out var actualKind, out var actualArgs));
            Assert.Equal(kind, actualKind);
            Assert.Equal(args.Length, actualArgs.Count);
        }
    }

    [Fact]
    public void DeserializesIncomingWireShapes()
    {
        (IpcCommon.IncomingMessageType Kind, string Json, int Count)[] messages =
        [
            (IpcCommon.IncomingMessageType.AttachPage, """["AttachPage","https://example.test/","https://example.test/"]""", 2),
            (IpcCommon.IncomingMessageType.BeginInvokeDotNet, """["BeginInvokeDotNet","1","App","Double",0,"[21]"]""", 5),
            (IpcCommon.IncomingMessageType.EndInvokeJS, """["EndInvokeJS",2,true,"[2,true,42]"]""", 3),
            (IpcCommon.IncomingMessageType.OnRenderCompleted, """["OnRenderCompleted",42,null]""", 2),
            (IpcCommon.IncomingMessageType.OnLocationChanged, """["OnLocationChanged","https://example.test/next","s",true]""", 3),
            (IpcCommon.IncomingMessageType.ReceiveByteArrayFromJS, """["ReceiveByteArrayFromJS",0,"AQID"]""", 2),
            (IpcCommon.IncomingMessageType.OnLocationChanging, """["OnLocationChanging",1,"https://example.test/next","s",false]""", 4),
        ];

        foreach (var (kind, json, count) in messages)
        {
            Assert.True(IpcCommon.TryDeserializeIncoming("__bwv:" + json, out var actualKind, out var args));
            Assert.Equal(kind, actualKind);
            Assert.Equal(count, args.Count);
            Assert.Equal("__bwv:" + json, IpcCommon.Serialize(kind, args.Select(arg => (object)arg).ToArray()));
        }

        Assert.False(IpcCommon.TryDeserializeIncoming("unrelated webview message", out _, out _));
        Assert.Throws<JsonException>(() => IpcCommon.TryDeserializeIncoming("__bwv:[", out _, out _));
    }
}
