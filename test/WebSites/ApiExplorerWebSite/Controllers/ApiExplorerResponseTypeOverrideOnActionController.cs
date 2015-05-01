// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ApiExplorerWebSite
{
    [Produces("application/json", Type = typeof(Product))]
    [Route("ApiExplorerResponseTypeOverrideOnAction")]
    public class ApiExplorerResponseTypeOverrideOnActionController : Controller
    {
        [HttpGet("Controller")]
        public void GetController()
        {
        }

        [HttpGet("Action")]
        [Produces(typeof(Customer))]
        public object GetAction()
        {
            return null;
        }
    }
}