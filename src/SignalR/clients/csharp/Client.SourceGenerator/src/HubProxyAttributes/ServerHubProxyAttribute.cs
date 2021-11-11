// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Provides information for generating strongly typed SignalR server invocations from the client.
/// Place this attribute on a method with the following syntax:
/// <code>
///   public static partial T GetProxy&lt;T&gt;(this HubConnection connection);
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerHubProxyAttribute : Attribute
{
}
