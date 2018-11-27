// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerNameSetByConvention")]
    public class ApiExplorerNameSetByConventionController : Controller
    {
        [HttpGet]
        public void Get()
        {
        }
    }
}