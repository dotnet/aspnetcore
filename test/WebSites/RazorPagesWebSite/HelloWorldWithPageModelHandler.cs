// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class HelloWorldWithPageModelHandler : PageModel
    {
        public string Message { get; set; }

        public void OnGet(string message)
        {
            Message = message;
        }

        public void OnPost()
        {
            Message = "You posted!";
        }
    }
}
