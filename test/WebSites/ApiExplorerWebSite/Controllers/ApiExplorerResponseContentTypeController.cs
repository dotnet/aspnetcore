// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System.Threading.Tasks;

namespace ApiExplorer
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
        [Produces("*/*")]
        public Product AllTypes()
        {
            return null;
        }

        [HttpGet]
        [Produces("text/*")]
        public Product Range()
        {
            return null;
        }

        [HttpGet]
        [Produces("application/json")]
        public Product Specific()
        {
            return null;
        }

        [HttpGet]
        [Produces("application/hal+json")]
        public Product NoMatch()
        {
            return null;
        }
    }
}