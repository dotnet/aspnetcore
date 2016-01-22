// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace FormatFilterSample.Web
{
    [FormatFilter]
    public class FormatFilterController : Controller
    {
        [Route("[controller]/[action]/{id}.{format?}")]
        [Route("[controller]/[action].{format}")]
        public Product GetProduct(int id = 0)
        {
            return new Product() { SampleInt = id };
        }

        [Produces("application/custom", "application/json", "text/json")]
        [Route("[controller]/[action]/{id}.{format}")]
        public Product ProducesMethod(int id)
        {
            return new Product() { SampleInt = id };
        }
    }
}