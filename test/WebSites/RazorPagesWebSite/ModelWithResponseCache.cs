// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Client)]
    public class ModelWithResponseCache : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = $"Hello from {nameof(ModelWithResponseCache)}.{nameof(OnGet)}";
        }
    }
}
