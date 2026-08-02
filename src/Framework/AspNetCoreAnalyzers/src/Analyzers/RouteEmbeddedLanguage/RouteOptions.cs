// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[Flags]
internal enum RouteOptions
{
    /// <summary>
    /// HTTP route. Used to match endpoints for Minimal API, MVC, SignalR, gRPC, etc.
    /// </summary> 
    Http = 0,
    /// <summary>
    /// Component route. Used to match Razor components for Blazor.
    /// </summary>
    Component = 1,
}
