// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

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
