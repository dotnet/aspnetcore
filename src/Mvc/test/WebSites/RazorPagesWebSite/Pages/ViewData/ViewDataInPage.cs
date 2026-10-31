// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

public class ViewDataInPage : PageModel
{
    [ViewData]
    public string Title => "Title with default value";

    [ViewData]
    public string Keywords { get; set; }

    [ViewData]
    public string Description { get; set; }

    [ViewData(Key = "Author")]
    public string AuthorName { get; set; }

    public void OnGet()
    {
        Description = "Description set in handler";
        AuthorName = "Property with key";
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        Keywords = "Value set in filter";
    }
}
