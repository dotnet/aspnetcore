// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class PolymorphicBinding : PageModel
    {
        [ModelBinder(typeof(PolymorphicModelBinder))]
        public IUserModel UserModel { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return new ContentResult { Content = UserModel.ToString() };
        }
    }
}
