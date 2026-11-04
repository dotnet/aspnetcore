// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using IdentitySample.Models;
using IdentitySample.Models.ManageViewModels;
using IdentitySample.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentitySamples.Controllers;

[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ILogger _logger;

    public ManageController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender emailSender,
    ISmsSender smsSender,
    ILoggerFactory loggerFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _logger = loggerFactory.CreateLogger<ManageController>();
    }

    //
    // GET: /Manage/Index
    [HttpGet]
    public async Task<IActionResult> Index(ManageMessageId? message = null)
    {
        ViewData["StatusMessage"] =
            message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
            : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
            : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
            : message == ManageMessageId.Error ? "An error has occurred."
            : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
            : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
            : "";

        var user = await GetCurrentUserAsync();
        var model = new IndexViewModel
        {
            HasPassword = await _userManager.HasPasswordAsync(user),
            PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
            TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
            Logins = await _userManager.GetLoginsAsync(user),
            BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user),
            AuthenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user)
        };
        return View(model);
    }

    //
    // POST: /Manage/RemoveLogin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account)
    {
        ManageMessageId? message = ManageMessageId.Error;
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await _userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                message = ManageMessageId.RemoveLoginSuccess;
            }
        }
        return RedirectToAction(nameof(ManageLogins), new { Message = message });
    }

    //
    // GET: /Manage/AddPhoneNumber
    public IActionResult AddPhoneNumber()
    {
        return View();
    }

    //
    // POST: /Manage/AddPhoneNumber
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        // Generate the token and send it
        var user = await GetCurrentUserAsync();
        var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
        await _smsSender.SendSmsAsync(model.PhoneNumber, "Your security code is: " + code);
        return RedirectToAction(nameof(VerifyPhoneNumber), new { PhoneNumber = model.PhoneNumber });
    }

    //
    // POST: /Manage/ResetAuthenticatorKey
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticatorKey()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation(1, "User reset authenticator key.");
        }
        return RedirectToAction(nameof(Index), "Manage");
    }

    //
    // POST: /Manage/GenerateRecoveryCode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCode()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 5);
            _logger.LogInformation(1, "User generated new recovery code.");
            return View("DisplayRecoveryCodes", new DisplayRecoveryCodesViewModel { Codes = codes });
        }
        return View("Error");
    }

    //
    // POST: /Manage/EnableTwoFactorAuthentication
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactorAuthentication()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation(1, "User enabled two-factor authentication.");
        }
        return RedirectToAction(nameof(Index), "Manage");
    }

    //
    // POST: /Manage/DisableTwoFactorAuthentication
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactorAuthentication()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation(2, "User disabled two-factor authentication.");
        }
        return RedirectToAction(nameof(Index), "Manage");
    }

    //
    // GET: /Manage/VerifyPhoneNumber
    [HttpGet]
    public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
    {
        var code = await _userManager.GenerateChangePhoneNumberTokenAsync(await GetCurrentUserAsync(), phoneNumber);
        // Send an SMS to verify the phone number
        return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
    }

    //
    // POST: /Manage/VerifyPhoneNumber
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
            }
        }
        // If we got this far, something failed, redisplay the form
        ModelState.AddModelError(string.Empty, "Failed to verify phone number");
        return View(model);
    }

    //
    // GET: /Manage/RemovePhoneNumber
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePhoneNumber()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await _userManager.SetPhoneNumberAsync(user, null);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
            }
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    //
    // POST: /Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(3, "User changed their password successfully.");
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/SetPassword
    [HttpGet]
    public IActionResult SetPassword()
    {
        return View();
    }

    //
    // POST: /Manage/SetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //GET: /Manage/ManageLogins
    [HttpGet]
    public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
    {
        ViewData["StatusMessage"] =
            message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
            : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
            : message == ManageMessageId.Error ? "An error has occurred."
            : "";
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return View("Error");
        }
        var userLogins = await _userManager.GetLoginsAsync(user);
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        var otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
        ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
        return View(new ManageLoginsViewModel
        {
            CurrentLogins = userLogins,
            OtherLogins = otherLogins
        });
    }

    //
    // POST: /Manage/LinkLogin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LinkLogin(string provider)
    {
        // Request a redirect to the external login provider to link a login for the current user
        var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
        return Challenge(properties, provider);
    }

    //
    // GET: /Manage/LinkLoginCallback
    [HttpGet]
    public async Task<ActionResult> LinkLoginCallback()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return View("Error");
        }
        var info = await _signInManager.GetExternalLoginInfoAsync(await _userManager.GetUserIdAsync(user));
        if (info == null)
        {
            return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
        }
        var result = await _userManager.AddLoginAsync(user, info);
        var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
        return RedirectToAction(nameof(ManageLogins), new { Message = message });
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public enum ManageMessageId
    {
        AddPhoneSuccess,
        AddLoginSuccess,
        ChangePasswordSuccess,
        SetTwoFactorSuccess,
        SetPasswordSuccess,
        RemoveLoginSuccess,
        RemovePhoneSuccess,
        Error
    }

    private Task<ApplicationUser> GetCurrentUserAsync()
    {
        return _userManager.GetUserAsync(HttpContext.User);
    }

    #endregion
}
