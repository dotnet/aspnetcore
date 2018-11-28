// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;

namespace RoutingWebSite
{
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
}