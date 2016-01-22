// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerVisbilityDisabledByConvention")]
    public class ApiExplorerVisbilityDisabledByConventionController : Controller
    {
        [HttpGet]
        public void Get()
        {
        }
    }
}