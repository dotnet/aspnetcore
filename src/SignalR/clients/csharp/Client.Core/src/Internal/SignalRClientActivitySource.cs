// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.SignalR.Client.Internal;

// Internal for now so we don't need API review.
// Just a wrapper for the ActivitySource. Don't want to put ActivitySource directly in DI as
// it is a public type and could conflict with activity source from another library.
internal sealed class SignalRClientActivitySource
{
    public static readonly SignalRClientActivitySource Instance = new();

    public ActivitySource ActivitySource { get; } = new ActivitySource("Microsoft.AspNetCore.SignalR.Client");
}
