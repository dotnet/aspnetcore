// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite.Controllers
{
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
}