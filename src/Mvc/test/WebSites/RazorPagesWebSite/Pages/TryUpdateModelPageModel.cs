// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

public class TryUpdateModelPageModel : PageModel
{
    public UserModel UserModel { get; set; }

    public bool Updated { get; set; }

    public async Task OnPost()
    {
        var user = new UserModel();
        Updated = await TryUpdateModelAsync(user);
        UserModel = user;
    }
}
