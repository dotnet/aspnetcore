// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.DefaultUI.WebSite.Pages;

public class ContactModel : PageModel
{
    public string Message { get; set; }

    public void OnGet()
    {
        Message = "Your contact page.";
    }
}
