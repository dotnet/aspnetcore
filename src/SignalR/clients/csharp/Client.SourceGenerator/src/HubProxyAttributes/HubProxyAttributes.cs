// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// Place this attribute on a method with the following syntax:
    /// <code>
    ///   public static partial IDisposable RegisterCallbacks&lt;T&gt;(this HubConnection connection, T proxy);
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HubClientProxyAttribute : Attribute
    {
    }

    /// <summary>
    /// Place this attribute on a method with the following syntax:
    /// <code>
    ///   public static partial T GetProxy&lt;T&gt;(this HubConnection connection);
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HubServerProxyAttribute : Attribute
    {
    }
}
