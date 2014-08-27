// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System.Threading.Tasks;

namespace ApiExplorer
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