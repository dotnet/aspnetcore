// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class PageModelWithPropertyAndArgumentBinding : PageModel
    {
        [ModelBinder]
        public UserModel UserModel { get; set; }

        public int Id { get; set; }

        public void OnGet(int id)
        {
            Id = id;
        }

        public void OnPost(int id)
        {
            Id = id;
        }
    }
}
