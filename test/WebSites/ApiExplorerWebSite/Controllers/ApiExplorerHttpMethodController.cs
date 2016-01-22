// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerHttpMethod")]
    public class ApiExplorerHttpMethodController : Controller
    {
        [Route("All")]
        public void All()
        {
        }

        [HttpGet("Get")]
        public void Get()
        {
        }

        [AcceptVerbs("PUT", "POST", Route = "Single")]
        public void PutOrPost()
        {
        }

        [HttpGet("MultipleActions")]
        [HttpPut("MultipleActions")]
        public void MultipleActions()
        {
        }
    }
}