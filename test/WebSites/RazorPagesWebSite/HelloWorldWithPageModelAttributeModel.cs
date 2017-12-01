// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace RazorPagesWebSite
{
    [PageModel]
    public class HelloWorldWithPageModelAttributeModel
    {
        public string Message { get; set; }

        public void OnGet(string message)
        {
            Message = message;
        }
    }
}
