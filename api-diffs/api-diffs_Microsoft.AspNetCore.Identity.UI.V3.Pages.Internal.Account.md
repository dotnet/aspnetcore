# Microsoft.AspNetCore.Identity.UI.V3.Pages.Internal.Account

``` diff
-namespace Microsoft.AspNetCore.Identity.UI.V3.Pages.Internal.Account {
 {
-    public class Areas_Identity_Pages_Account__ViewImports : RazorPage<object> {
 {
-        public Areas_Identity_Pages_Account__ViewImports();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_AccessDenied : Page {
 {
-        public Areas_Identity_Pages_Account_AccessDenied();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<AccessDeniedModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public AccessDeniedModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<AccessDeniedModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ConfirmEmail : Page {
 {
-        public Areas_Identity_Pages_Account_ConfirmEmail();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ConfirmEmailModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ConfirmEmailModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ConfirmEmailModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ExternalLogin : Page {
 {
-        public Areas_Identity_Pages_Account_ExternalLogin();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ExternalLoginModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ExternalLoginModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ExternalLoginModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ForgotPassword : Page {
 {
-        public Areas_Identity_Pages_Account_ForgotPassword();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ForgotPasswordModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ForgotPasswordModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ForgotPasswordModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ForgotPasswordConfirmation : Page {
 {
-        public Areas_Identity_Pages_Account_ForgotPasswordConfirmation();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ForgotPasswordConfirmation> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ForgotPasswordConfirmation Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ForgotPasswordConfirmation> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Lockout : Page {
 {
-        public Areas_Identity_Pages_Account_Lockout();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<LockoutModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public LockoutModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<LockoutModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Login : Page {
 {
-        public Areas_Identity_Pages_Account_Login();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<LoginModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public LoginModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<LoginModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_LoginWith2fa : Page {
 {
-        public Areas_Identity_Pages_Account_LoginWith2fa();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<LoginWith2faModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public LoginWith2faModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<LoginWith2faModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_LoginWithRecoveryCode : Page {
 {
-        public Areas_Identity_Pages_Account_LoginWithRecoveryCode();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<LoginWithRecoveryCodeModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public LoginWithRecoveryCodeModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<LoginWithRecoveryCodeModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Logout : Page {
 {
-        public Areas_Identity_Pages_Account_Logout();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<LogoutModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public LogoutModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<LogoutModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Register : Page {
 {
-        public Areas_Identity_Pages_Account_Register();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<RegisterModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public RegisterModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<RegisterModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ResetPassword : Page {
 {
-        public Areas_Identity_Pages_Account_ResetPassword();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ResetPasswordModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ResetPasswordModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ResetPasswordModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_ResetPasswordConfirmation : Page {
 {
-        public Areas_Identity_Pages_Account_ResetPasswordConfirmation();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ResetPasswordConfirmationModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ResetPasswordConfirmationModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ResetPasswordConfirmationModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-}
```

