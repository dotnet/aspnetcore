// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages
{
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
}
