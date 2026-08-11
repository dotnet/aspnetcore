// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BlazorServerAotSample.Pages;

public sealed class AotPayloadEventArgs : EventArgs
{
    public string Message { get; set; } = "";

    public Animal? Payload { get; set; }
}

[EventHandler("onaotpayload", typeof(AotPayloadEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
