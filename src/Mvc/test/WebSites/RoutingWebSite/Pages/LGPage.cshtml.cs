// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasicWebSite.Pages;

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
