// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BasicTestApp.CustomEventTypesNamespace;

[EventHandler("ontestevent", typeof(TestEventArgs), true, true)]
[EventHandler("onkeydown.testvariant", typeof(TestKeyDownEventArgs), true, true)]
[EventHandler("onkeydown.yetanother", typeof(YetAnotherCustomKeyboardEventArgs), true, true)]
[EventHandler("oncustommouseover", typeof(EventArgs), true, true)]
[EventHandler("onsendjsobject", typeof(EventWithCustomSerializedDataEventArgs), true, true)]
[EventHandler("oncontentblur", typeof(ContentEditableEventArgs), true, true)]
public static class EventHandlers
{
}

class TestEventArgs : EventArgs
{
    public string MyProp { get; set; }
}

class TestKeyDownEventArgs : EventArgs
{
    public string CustomKeyInfo { get; set; }
}

class YetAnotherCustomKeyboardEventArgs : EventArgs
{
    public string YouPressed { get; set; }
}

class EventWithCustomSerializedDataEventArgs : EventArgs
{
    public IJSObjectReference JsObject { get; set; }
    public DotNetObjectReference<DotNetType> DotNetObject { get; set; }
    public byte[] ByteArray { get; set; }
}

class DotNetType
{
    // Deliberately not creatable through JSON deserialization
    // since we want to show it's the original .NET object instance
    public string Property { get; }

    public DotNetType(string propertyValue)
    {
        Property = propertyValue;
    }
}

/// <summary>
/// Event arguments for contenteditable elements that capture both textContent and innerHTML.
/// </summary>
class ContentEditableEventArgs : EventArgs
{
    public string TextContent { get; set; }
    public string InnerHTML { get; set; }
}
