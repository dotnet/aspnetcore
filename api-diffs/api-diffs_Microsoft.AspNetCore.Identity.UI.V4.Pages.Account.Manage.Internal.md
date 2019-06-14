# Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal

``` diff
-namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal {
 {
-    public abstract class ChangePasswordModel : PageModel {
 {
-        protected ChangePasswordModel();

-        public ChangePasswordModel.InputModel Input { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string ConfirmPassword { get; set; }

-            public string NewPassword { get; set; }

-            public string OldPassword { get; set; }

-        }
-    }
-    public abstract class DeletePersonalDataModel : PageModel {
 {
-        protected DeletePersonalDataModel();

-        public DeletePersonalDataModel.InputModel Input { get; set; }

-        public bool RequirePassword { get; set; }

-        public virtual Task<IActionResult> OnGet();

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string Password { get; set; }

-        }
-    }
-    public abstract class Disable2faModel : PageModel {
 {
-        protected Disable2faModel();

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGet();

-        public virtual Task<IActionResult> OnPostAsync();

-    }
-    public abstract class DownloadPersonalDataModel : PageModel {
 {
-        protected DownloadPersonalDataModel();

-        public virtual IActionResult OnGet();

-        public virtual Task<IActionResult> OnPostAsync();

-    }
-    public class EnableAuthenticatorModel : PageModel {
 {
-        public EnableAuthenticatorModel();

-        public string AuthenticatorUri { get; set; }

-        public EnableAuthenticatorModel.InputModel Input { get; set; }

-        public string[] RecoveryCodes { get; set; }

-        public string SharedKey { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string Code { get; set; }

-        }
-    }
-    public abstract class ExternalLoginsModel : PageModel {
 {
-        protected ExternalLoginsModel();

-        public IList<UserLoginInfo> CurrentLogins { get; set; }

-        public IList<AuthenticationScheme> OtherLogins { get; set; }

-        public bool ShowRemoveButton { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnGetLinkLoginCallbackAsync();

-        public virtual Task<IActionResult> OnPostLinkLoginAsync(string provider);

-        public virtual Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey);

-    }
-    public abstract class GenerateRecoveryCodesModel : PageModel {
 {
-        protected GenerateRecoveryCodesModel();

-        public string[] RecoveryCodes { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-    }
-    public abstract class IndexModel : PageModel {
 {
-        protected IndexModel();

-        public IndexModel.InputModel Input { get; set; }

-        public bool IsEmailConfirmed { get; set; }

-        public string StatusMessage { get; set; }

-        public string Username { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-        public virtual Task<IActionResult> OnPostSendVerificationEmailAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string Email { get; set; }

-            public string PhoneNumber { get; set; }

-        }
-    }
-    public static class ManageNavPages {
 {
-        public static string ChangePassword { get; }

-        public static string DeletePersonalData { get; }

-        public static string DownloadPersonalData { get; }

-        public static string ExternalLogins { get; }

-        public static string Index { get; }

-        public static string PersonalData { get; }

-        public static string TwoFactorAuthentication { get; }

-        public static string ChangePasswordNavClass(ViewContext viewContext);

-        public static string DeletePersonalDataNavClass(ViewContext viewContext);

-        public static string DownloadPersonalDataNavClass(ViewContext viewContext);

-        public static string ExternalLoginsNavClass(ViewContext viewContext);

-        public static string IndexNavClass(ViewContext viewContext);

-        public static string PageNavClass(ViewContext viewContext, string page);

-        public static string PersonalDataNavClass(ViewContext viewContext);

-        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext);

-    }
-    public abstract class PersonalDataModel : PageModel {
 {
-        protected PersonalDataModel();

-        public virtual Task<IActionResult> OnGet();

-    }
-    public abstract class ResetAuthenticatorModel : PageModel {
 {
-        protected ResetAuthenticatorModel();

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGet();

-        public virtual Task<IActionResult> OnPostAsync();

-    }
-    public abstract class SetPasswordModel : PageModel {
 {
-        protected SetPasswordModel();

-        public SetPasswordModel.InputModel Input { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string ConfirmPassword { get; set; }

-            public string NewPassword { get; set; }

-        }
-    }
-    public class ShowRecoveryCodesModel : PageModel {
 {
-        public ShowRecoveryCodesModel();

-        public string[] RecoveryCodes { get; set; }

-        public string StatusMessage { get; set; }

-        public IActionResult OnGet();

-    }
-    public abstract class TwoFactorAuthenticationModel : PageModel {
 {
-        protected TwoFactorAuthenticationModel();

-        public bool HasAuthenticator { get; set; }

-        public bool Is2faEnabled { get; set; }

-        public bool IsMachineRemembered { get; set; }

-        public int RecoveryCodesLeft { get; set; }

-        public string StatusMessage { get; set; }

-        public virtual Task<IActionResult> OnGetAsync();

-        public virtual Task<IActionResult> OnPostAsync();

-    }
-}
```

