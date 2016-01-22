// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Produces("text/xml")]
    [Route("ApiExplorerResponseContentTypeOverrideOnAction")]
    public class ApiExplorerResponseContentTypeOverrideOnActionController : Controller
    {
        [HttpGet("Controller")]
        public Product GetController()
        {
            return null;
        }

        [HttpGet("Action")]
        [Produces("application/json")]
        public Product GetAction()
        {
            return null;
        }
    }
}