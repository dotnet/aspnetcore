// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerResponseContentType/[Action]")]
    public class ApiExplorerResponseContentTypeController : Controller
    {
        [HttpGet]
        public Product Unset()
        {
            return null;
        }

        [HttpGet]
        [Produces("application/json", "text/json")]
        public Product Specific()
        {
            return null;
        }

        [HttpGet]
        [Produces("application/hal+json", "text/hal+json")]
        public Product NoMatch()
        {
            return null;
        }
    }
}