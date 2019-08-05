// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    public static partial class IdentityBuilderUIExtensions
    {
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddDefaultUI(this Microsoft.AspNetCore.Identity.IdentityBuilder builder) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Identity.UI
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false, AllowMultiple=false)]
    public sealed partial class UIFrameworkAttribute : System.Attribute
    {
        public UIFrameworkAttribute(string uiFramework) { }
        public string UIFramework { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.Services
{
    public partial interface IEmailSender
    {
        System.Threading.Tasks.Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal
{
    public partial class AccessDeniedModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public AccessDeniedModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ConfirmEmailChangeModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ConfirmEmailChangeModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string userId, string email, string code) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ConfirmEmailModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ConfirmEmailModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string userId, string code) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ExternalLoginModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ExternalLoginModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.ExternalLoginModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ProviderDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnPost(string provider, string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostConfirmationAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ForgotPasswordConfirmation : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ForgotPasswordConfirmation() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ForgotPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ForgotPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.ForgotPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class LockoutModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public LockoutModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.LoginModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostSendVerificationEmailAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Remember me?")]
            public bool RememberMe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginWith2faModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginWith2faModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.LoginWith2faModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RememberMe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Remember this machine")]
            public bool RememberMachine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Authenticator code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(7, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string TwoFactorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginWithRecoveryCodeModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginWithRecoveryCodeModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.LoginWithRecoveryCodeModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Recovery Code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string RecoveryCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LogoutModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LogoutModel() { }
        public void OnGet() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPost(string returnUrl = null) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class RegisterConfirmationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public RegisterConfirmationModel() { }
        public bool DisplayConfirmAccountLink { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EmailConfirmationUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string email) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class RegisterModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected RegisterModel() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.RegisterModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("Password", ErrorMessage="The password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Email")]
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ResetPasswordConfirmationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ResetPasswordConfirmationModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ResetPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ResetPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal.ResetPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet(string code = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.CompareAttribute("Password", ErrorMessage="The password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal
{
    public abstract partial class ChangePasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ChangePasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.ChangePasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("NewPassword", ErrorMessage="The new password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm new password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string NewPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Current password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string OldPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class DeletePersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected DeletePersonalDataModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.DeletePersonalDataModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequirePassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class Disable2faModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected Disable2faModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class DownloadPersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected DownloadPersonalDataModel() { }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class EmailModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected EmailModel() { }
        public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.EmailModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsEmailConfirmed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostChangeEmailAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostSendVerificationEmailAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New email")]
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string NewEmail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class EnableAuthenticatorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public EnableAuthenticatorModel() { }
        public string AuthenticatorUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.EnableAuthenticatorModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SharedKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Verification Code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(7, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class ExternalLoginsModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ExternalLoginsModel() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo> CurrentLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> OtherLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShowRemoveButton { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetLinkLoginCallbackAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostLinkLoginAsync(string provider) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey) { throw null; }
    }
    public abstract partial class GenerateRecoveryCodesModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected GenerateRecoveryCodesModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected IndexModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.IndexModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Username { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Phone number")]
            [System.ComponentModel.DataAnnotations.PhoneAttribute]
            public string PhoneNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public static partial class ManageNavPages
    {
        public static string ChangePassword { get { throw null; } }
        public static string DeletePersonalData { get { throw null; } }
        public static string DownloadPersonalData { get { throw null; } }
        public static string Email { get { throw null; } }
        public static string ExternalLogins { get { throw null; } }
        public static string Index { get { throw null; } }
        public static string PersonalData { get { throw null; } }
        public static string TwoFactorAuthentication { get { throw null; } }
        public static string ChangePasswordNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string DeletePersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string DownloadPersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string EmailNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string ExternalLoginsNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string IndexNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string PageNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string page) { throw null; }
        public static string PersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string TwoFactorAuthenticationNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
    }
    public abstract partial class PersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected PersonalDataModel() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
    }
    public abstract partial class ResetAuthenticatorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ResetAuthenticatorModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class SetPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected SetPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Manage.Internal.SetPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("NewPassword", ErrorMessage="The new password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm new password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string NewPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class ShowRecoveryCodesModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ShowRecoveryCodesModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
    }
    public abstract partial class TwoFactorAuthenticationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected TwoFactorAuthenticationModel() { }
        public bool HasAuthenticator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public bool Is2faEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsMachineRemembered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RecoveryCodesLeft { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V3.Pages.Internal
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    [Microsoft.AspNetCore.Mvc.ResponseCacheAttribute(Duration=0, Location=Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None, NoStore=true)]
    public partial class ErrorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ErrorModel() { }
        public string RequestId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShowRequestId { get { throw null; } }
        public void OnGet() { }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal
{
    public partial class AccessDeniedModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public AccessDeniedModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ConfirmEmailChangeModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ConfirmEmailChangeModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string userId, string email, string code) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ConfirmEmailModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ConfirmEmailModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string userId, string code) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ExternalLoginModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ExternalLoginModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.ExternalLoginModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ProviderDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null) { throw null; }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnPost(string provider, string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostConfirmationAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ForgotPasswordConfirmation : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ForgotPasswordConfirmation() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ForgotPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ForgotPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.ForgotPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class LockoutModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public LockoutModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.LoginModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Remember me?")]
            public bool RememberMe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginWith2faModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginWith2faModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.LoginWith2faModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RememberMe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Remember this machine")]
            public bool RememberMachine { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Authenticator code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(7, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string TwoFactorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LoginWithRecoveryCodeModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LoginWithRecoveryCodeModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.LoginWithRecoveryCodeModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Recovery Code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string RecoveryCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class LogoutModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected LogoutModel() { }
        public void OnGet() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPost(string returnUrl = null) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class RegisterConfirmationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public RegisterConfirmationModel() { }
        public bool DisplayConfirmAccountLink { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EmailConfirmationUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync(string email) { throw null; }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class RegisterModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected RegisterModel() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.RegisterModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReturnUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task OnGetAsync(string returnUrl = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync(string returnUrl = null) { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("Password", ErrorMessage="The password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Email")]
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class ResetPasswordConfirmationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ResetPasswordConfirmationModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public abstract partial class ResetPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ResetPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal.ResetPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet(string code = null) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.CompareAttribute("Password", ErrorMessage="The password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal
{
    public abstract partial class ChangePasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ChangePasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.ChangePasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("NewPassword", ErrorMessage="The new password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm new password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string NewPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Current password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string OldPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class DeletePersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected DeletePersonalDataModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.DeletePersonalDataModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequirePassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class Disable2faModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected Disable2faModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class DownloadPersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected DownloadPersonalDataModel() { }
        public virtual Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class EmailModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected EmailModel() { }
        public string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.EmailModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsEmailConfirmed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostChangeEmailAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostSendVerificationEmailAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New email")]
            [System.ComponentModel.DataAnnotations.EmailAddressAttribute]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            public string NewEmail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class EnableAuthenticatorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public EnableAuthenticatorModel() { }
        public string AuthenticatorUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.EnableAuthenticatorModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SharedKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Text)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Verification Code")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(7, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public abstract partial class ExternalLoginsModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ExternalLoginsModel() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo> CurrentLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> OtherLogins { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShowRemoveButton { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetLinkLoginCallbackAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostLinkLoginAsync(string provider) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey) { throw null; }
    }
    public abstract partial class GenerateRecoveryCodesModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected GenerateRecoveryCodesModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected IndexModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.IndexModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Username { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Phone number")]
            [System.ComponentModel.DataAnnotations.PhoneAttribute]
            public string PhoneNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public static partial class ManageNavPages
    {
        public static string ChangePassword { get { throw null; } }
        public static string DeletePersonalData { get { throw null; } }
        public static string DownloadPersonalData { get { throw null; } }
        public static string Email { get { throw null; } }
        public static string ExternalLogins { get { throw null; } }
        public static string Index { get { throw null; } }
        public static string PersonalData { get { throw null; } }
        public static string TwoFactorAuthentication { get { throw null; } }
        public static string ChangePasswordNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string DeletePersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string DownloadPersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string EmailNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string ExternalLoginsNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string IndexNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string PageNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext, string page) { throw null; }
        public static string PersonalDataNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
        public static string TwoFactorAuthenticationNavClass(Microsoft.AspNetCore.Mvc.Rendering.ViewContext viewContext) { throw null; }
    }
    public abstract partial class PersonalDataModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected PersonalDataModel() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
    }
    public abstract partial class ResetAuthenticatorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected ResetAuthenticatorModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGet() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
    public abstract partial class SetPasswordModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected SetPasswordModel() { }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal.SetPasswordModel.InputModel Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
        public partial class InputModel
        {
            public InputModel() { }
            [System.ComponentModel.DataAnnotations.CompareAttribute("NewPassword", ErrorMessage="The new password and confirmation password do not match.")]
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="Confirm new password")]
            public string ConfirmPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            [System.ComponentModel.DataAnnotations.DataTypeAttribute(System.ComponentModel.DataAnnotations.DataType.Password)]
            [System.ComponentModel.DataAnnotations.DisplayAttribute(Name="New password")]
            [System.ComponentModel.DataAnnotations.RequiredAttribute]
            [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, ErrorMessage="The {0} must be at least {2} and at max {1} characters long.", MinimumLength=6)]
            public string NewPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
    public partial class ShowRecoveryCodesModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ShowRecoveryCodesModel() { }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string[] RecoveryCodes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
    }
    public abstract partial class TwoFactorAuthenticationModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        protected TwoFactorAuthenticationModel() { }
        public bool HasAuthenticator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.BindPropertyAttribute]
        public bool Is2faEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsMachineRemembered { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RecoveryCodesLeft { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.TempDataAttribute]
        public string StatusMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Internal
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    [Microsoft.AspNetCore.Mvc.ResponseCacheAttribute(Duration=0, Location=Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None, NoStore=true)]
    public partial class ErrorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ErrorModel() { }
        public string RequestId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShowRequestId { get { throw null; } }
        public void OnGet() { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class IdentityServiceCollectionUIExtensions
    {
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddDefaultIdentity<TUser>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TUser : class { throw null; }
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddDefaultIdentity<TUser>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Identity.IdentityOptions> configureOptions) where TUser : class { throw null; }
    }
}
