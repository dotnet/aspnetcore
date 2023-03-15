// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// A function that handles an inbound <see cref="Circuit"/> event.
/// </summary>
/// <param name="context">The <see cref="CircuitInboundEventContext"/> for the event.</param>
/// <returns>A <see cref="Task"/> that completes when the event has finished.</returns>
public delegate Task CircuitInboundEventDelegate(in CircuitInboundEventContext context);
