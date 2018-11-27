// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite.Controllers
{
    [Route("ApiExplorerParameters/[action]")]
    public class ApiExplorerParametersController : Controller
    {
        public void SimpleParameters(int i, string s)
        {
        }

        public void SimpleParametersWithBinderMetadata([FromQuery] int i, [FromRoute] string s)
        {
        }

        public void SimpleModel(Product product)
        {
        }

        [Route("{id}")]
        public void SimpleModelFromBody(int id, [FromBody] Product product)
        {
        }

        public void ComplexModel([FromQuery] OrderDTO order)
        {
        }
    }
}