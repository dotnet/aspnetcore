// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace RazorPagesWebSite;

[PageModel]
public class HelloWorldWithPageModelAttributeModel
{
    public string Message { get; set; }

    public void OnGet(string message)
    {
        Message = message;
    }
}
