// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Produces("application/json", Type = typeof(Product))]
    [ProducesResponseType(typeof(ErrorInfo), 500)]
    [Route("ApiExplorerResponseTypeOverrideOnAction")]
    public class ApiExplorerResponseTypeOverrideOnActionController : Controller
    {
        [HttpGet("Controller")]
        public void GetController()
        {
        }

        [HttpGet("Action")]
        [Produces(typeof(Customer))]
        [ProducesResponseType(typeof(ErrorInfoOverride), 500)] // overriding the type specified on the server
        public object GetAction()
        {
            return null;
        }
    }

    public class ErrorInfo
    {
        public string Message { get; set; }
    }

    public class ErrorInfoOverride { }
}