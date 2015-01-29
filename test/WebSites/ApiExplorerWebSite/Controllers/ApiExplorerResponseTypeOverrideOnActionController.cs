// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ApiExplorerWebSite
{
    [Produces("*/*", Type = typeof(Product))]
    [Route("ApiExplorerResponseTypeOverrideOnAction")]
    public class ApiExplorerResponseTypeOverrideOnActionController : Controller
    {
        [HttpGet("Controller")]
        public void GetController()
        {
        }

        [HttpGet("Action")]
        [ProducesType(typeof(Customer))]
        public object GetAction()
        {
            return null;
        }
    }
}