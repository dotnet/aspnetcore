// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace CorsMiddlewareWebSite
{
    [Route("CorsMiddleWare/[action]")]
    public class BlogController : Controller
    {
        public string GetExclusiveContent()
        {
            return "exclusive";
        }
    }
}