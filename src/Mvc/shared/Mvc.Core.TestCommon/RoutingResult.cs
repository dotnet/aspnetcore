// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

// See TestResponseGenerator for the code that generates this data.
public class RoutingResult
{
    public string[] ExpectedUrls { get; set; }

    public string ActualUrl { get; set; }

    public Dictionary<string, object> RouteValues { get; set; }

    public string RouteName { get; set; }

    public string Action { get; set; }

    public string Controller { get; set; }

    public string Link { get; set; }
}
