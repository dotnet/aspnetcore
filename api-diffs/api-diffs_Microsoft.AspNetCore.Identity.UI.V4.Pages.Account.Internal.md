# Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal

``` diff
-namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal {
 {
-    public class AccessDeniedModel : PageModel {
 {
-        public AccessDeniedModel();

-        public void OnGet();

-    }
-    public abstract class ConfirmEmailModel : PageModel {
 {
-        protected ConfirmEmailModel();

-        public virtual Task<IActionResult> OnGetAsync(string userId, string code);

-    }
-    public class ExternalLoginModel : PageModel {
 {
-        public ExternalLoginModel();

-        public string ErrorMessage { get; set; }

-        public ExternalLoginModel.InputModel Input { get; set; }

-        public string ProviderDisplayName { get; set; }

-        public string ReturnUrl { get; set; }

-        public virtual IActionResult OnGet();

-        public virtual Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null);

-        public virtual IActionResult OnPost(string provider, string returnUrl = null);

-        public virtual Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null);

-        public class InputModel {
 {
-            public InputModel();

-            public string Email { get; set; }

-        }
-    }
-    public class ForgotPasswordConfirmation : PageModel {
 {
-        public ForgotPasswordConfirmation();

-        public void OnGet();

-    }
-    public abstract class ForgotPasswordModel : PageModel {
 {
-        protected ForgotPasswordModel();

-        public ForgotPasswordModel.InputModel Input { get; set; }

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string Email { get; set; }

-        }
-    }
-    public class LockoutModel : PageModel {
 {
-        public LockoutModel();

-        public void OnGet();

-    }
-    public abstract class LoginModel : PageModel {
 {
-        protected LoginModel();

-        public string ErrorMessage { get; set; }

-        public IList<AuthenticationScheme> ExternalLogins { get; set; }

-        public LoginModel.InputModel Input { get; set; }

-        public string ReturnUrl { get; set; }

-        public virtual Task OnGetAsync(string returnUrl = null);

-        public virtual Task<IActionResult> OnPostAsync(string returnUrl = null);

-        public class InputModel {
 {
-            public InputModel();

-            public string Email { get; set; }

-            public string Password { get; set; }

-            public bool RememberMe { get; set; }

-        }
-    }
-    public abstract class LoginWith2faModel : PageModel {
 {
-        protected LoginWith2faModel();

-        public LoginWith2faModel.InputModel Input { get; set; }

-        public bool RememberMe { get; set; }

-        public string ReturnUrl { get; set; }

-        public virtual Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null);

-        public virtual Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null);

-        public class InputModel {
 {
-            public InputModel();

-            public bool RememberMachine { get; set; }

-            public string TwoFactorCode { get; set; }

-        }
-    }
-    public abstract class LoginWithRecoveryCodeModel : PageModel {
 {
-        protected LoginWithRecoveryCodeModel();

-        public LoginWithRecoveryCodeModel.InputModel Input { get; set; }

-        public string ReturnUrl { get; set; }

-        public virtual Task<IActionResult> OnGetAsync(string returnUrl = null);

-        public virtual Task<IActionResult> OnPostAsync(string returnUrl = null);

-        public class InputModel {
 {
-            public InputModel();

-            public string RecoveryCode { get; set; }

-        }
-    }
-    public abstract class LogoutModel : PageModel {
 {
-        protected LogoutModel();

-        public void OnGet();

-        public virtual Task<IActionResult> OnPost(string returnUrl = null);

-    }
-    public abstract class RegisterModel : PageModel {
 {
-        protected RegisterModel();

-        public RegisterModel.InputModel Input { get; set; }

-        public string ReturnUrl { get; set; }

-        public virtual void OnGet(string returnUrl = null);

-        public virtual Task<IActionResult> OnPostAsync(string returnUrl = null);

-        public class InputModel {
 {
-            public InputModel();

-            public string ConfirmPassword { get; set; }

-            public string Email { get; set; }

-            public string Password { get; set; }

-        }
-    }
-    public class ResetPasswordConfirmationModel : PageModel {
 {
-        public ResetPasswordConfirmationModel();

-        public void OnGet();

-    }
-    public abstract class ResetPasswordModel : PageModel {
 {
-        protected ResetPasswordModel();

-        public ResetPasswordModel.InputModel Input { get; set; }

-        public virtual IActionResult OnGet(string code = null);

-        public virtual Task<IActionResult> OnPostAsync();

-        public class InputModel {
 {
-            public InputModel();

-            public string Code { get; set; }

-            public string ConfirmPassword { get; set; }

-            public string Email { get; set; }

-            public string Password { get; set; }

-        }
-    }
-}
```

