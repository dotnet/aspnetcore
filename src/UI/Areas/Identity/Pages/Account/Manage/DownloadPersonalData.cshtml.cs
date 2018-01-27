// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Identity.UI.Pages.Account.Manage.Internal
{
    [IdentityDefaultUI(typeof(DownloadPersonalDataModel<>))]
    public abstract class DownloadPersonalDataModel : PageModel
    {
        public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
    }

    internal class DownloadPersonalDataModel<TUser> : DownloadPersonalDataModel where TUser : IdentityUser
    {
        private readonly UserManager<TUser> _userManager;
        private readonly ILogger<DownloadPersonalDataModel> _logger;

        public DownloadPersonalDataModel(
            UserManager<TUser> userManager,
            ILogger<DownloadPersonalDataModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public override async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            _logger.LogInformation("User with ID '{UserId}' asked for their personal data.", _userManager.GetUserId(User));

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            personalData.Add("UserId", await _userManager.GetUserIdAsync(user));
            personalData.Add("UserName", await _userManager.GetUserNameAsync(user));
            personalData.Add("Email", await _userManager.GetEmailAsync(user));
            personalData.Add("EmailConfirmed", (await _userManager.IsEmailConfirmedAsync(user)).ToString());
            personalData.Add("PhoneNumber", await _userManager.GetPhoneNumberAsync(user));
            personalData.Add("PhoneNumberConfirmed", (await _userManager.IsPhoneNumberConfirmedAsync(user)).ToString());

            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(personalData)), "text/json");
        }
    }
}
