// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal
{
    [AllowAnonymous]
    [IdentityDefaultUI(typeof(ConfirmEmailModel<>))]
    public abstract class ConfirmEmailModel : PageModel
    {
        public virtual Task<IActionResult> OnGetAsync(string userId, string code) => throw new NotImplementedException();
    }

    internal class ConfirmEmailModel<TUser> : ConfirmEmailModel where TUser : class
    {
        private readonly UserManager<TUser> _userManager;

        public ConfirmEmailModel(UserManager<TUser> userManager)
        {
            _userManager = userManager;
        }

        public override async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Error confirming email for user with ID '{userId}':");
            }

            return Page();
        }
    }
}
