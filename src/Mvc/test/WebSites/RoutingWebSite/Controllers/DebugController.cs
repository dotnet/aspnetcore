// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Internal;

namespace RoutingWebSite;

// This controller is reachable via traditional routing.
public class DebugController : Controller
{
    private readonly DfaGraphWriter _graphWriter;
    private readonly EndpointDataSource _endpointDataSource;

    public DebugController(DfaGraphWriter graphWriter, EndpointDataSource endpointDataSource)
    {
        _graphWriter = graphWriter;
        _endpointDataSource = endpointDataSource;
    }

    public IActionResult Graph()
    {
        var sw = new StringWriter();
        _graphWriter.Write(_endpointDataSource, sw);

        return Content(sw.ToString());
    }
}
