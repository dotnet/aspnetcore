// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite
{
    public class StartupWithoutEndpointRouting : Startup
    {
        protected override void ConfigureMvcOptions(MvcOptions options)
        {
            options.EnableEndpointRouting = false;
        }
    }
}
