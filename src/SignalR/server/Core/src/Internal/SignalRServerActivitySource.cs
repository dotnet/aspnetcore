// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.SignalR.Internal;

// Internal for now so we don't need API review.
// Just a wrapper for the ActivitySource
// don't want to put ActivitySource directly in DI as hosting already does that and it could get overwritten.
internal sealed class SignalRServerActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.SignalR.Server";
    internal const string InvocationIn = $"{Name}.InvocationIn";
    internal const string OnConnected = $"{Name}.OnConnected";
    internal const string OnDisconnected = $"{Name}.OnDisconnected";

    public ActivitySource ActivitySource { get; } = new ActivitySource(Name);
}
