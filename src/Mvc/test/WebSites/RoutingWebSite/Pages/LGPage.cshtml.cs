// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace BasicWebSite.Pages
{
    public class LGPageModel : PageModel
    {
        private readonly LinkGenerator _linkGenerator;

        public LGPageModel(LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
        }

        public ContentResult OnGet()
        {
            return Content(_linkGenerator.GetPathByPage(HttpContext, "./LGAnotherPage"));
        }
    }
}