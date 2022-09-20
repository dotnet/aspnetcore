// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Template;

namespace Swaggatherer;

internal sealed class RouteEntry
{
    public RouteTemplate Template { get; set; }
    public string Method { get; set; }
    public decimal Precedence { get; set; }
    public string RequestUrl { get; set; }
}
