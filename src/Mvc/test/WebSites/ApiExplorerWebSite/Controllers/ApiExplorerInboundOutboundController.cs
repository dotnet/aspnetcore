// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite.Controllers;

public class ApiExplorerInboundOutBoundController : Controller
{
    [HttpGet("ApiExplorerInboundOutbound/SuppressedForLinkGeneration")]
    public void SuppressedForLinkGeneration()
    {
    }

    [HttpGet("ApiExplorerInboundOutbound/SuppressedForPathMatching")]
    public void SuppressedForPathMatching()
    {
    }
}
