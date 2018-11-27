// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages
{
    public class TryValidateModelPageModel : PageModel
    {
        [ModelBinder]
        public UserModel UserModel { get; set; }

        public bool Validate { get; set; }

        public void OnPost(UserModel user)
        {
            Validate = TryValidateModel(user);
        }
    }
}
