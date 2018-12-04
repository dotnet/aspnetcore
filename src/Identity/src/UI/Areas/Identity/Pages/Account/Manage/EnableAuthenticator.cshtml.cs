// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI.Pages.Account.Manage.Internal
{
    [IdentityDefaultUI(typeof(EnableAuthenticatorModel<>))]
    public class EnableAuthenticatorModel : PageModel
    {
        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }

        [TempData]
        public string[] RecoveryCodes { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }
        }

        public virtual Task<IActionResult> OnGetAsync() => throw new NotImplementedException();

        public virtual Task<IActionResult> OnPostAsync() => throw new NotImplementedException();
    }

    internal class EnableAuthenticatorModel<TUser> : EnableAuthenticatorModel where TUser : class
    {
        private readonly UserManager<TUser> _userManager;
        private readonly ILogger<EnableAuthenticatorModel> _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public EnableAuthenticatorModel(
            UserManager<TUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        public override async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);

            return Page();
        }

        public override async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Strip spaces and hypens
            var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

            StatusMessage = "Your authenticator app has been verified.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes.ToArray();
                return RedirectToPage("./ShowRecoveryCodes");
            }
            else
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(TUser user)
        {
            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);

            var email = await _userManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Microsoft.AspNetCore.Identity.UI"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}
