// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// Marks a method for use by SignalR source generator for serving strongly-typed hub proxies
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Method)]
    public class GetProxyAttribute : System.Attribute
    {
    }
}
