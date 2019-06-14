# Microsoft.AspNetCore.Identity.UI.V4.Pages.Internal.Account.Manage

``` diff
-namespace Microsoft.AspNetCore.Identity.UI.V4.Pages.Internal.Account.Manage {
 {
-    public class Areas_Identity_Pages_Account_Manage__Layout : RazorPage<object> {
 {
-        public Areas_Identity_Pages_Account_Manage__Layout();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage__ManageNav : RazorPage<object> {
 {
-        public Areas_Identity_Pages_Account_Manage__ManageNav();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage__StatusMessage : RazorPage<string> {
 {
-        public Areas_Identity_Pages_Account_Manage__StatusMessage();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<string> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage__ViewImports : RazorPage<object> {
 {
-        public Areas_Identity_Pages_Account_Manage__ViewImports();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage__ViewStart : RazorPage<object> {
 {
-        public Areas_Identity_Pages_Account_Manage__ViewStart();

-        public IViewComponentHelper Component { get; private set; }

-        public dynamic Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_ChangePassword : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_ChangePassword();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ChangePasswordModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ChangePasswordModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ChangePasswordModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_DeletePersonalData : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_DeletePersonalData();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<DeletePersonalDataModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public DeletePersonalDataModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<DeletePersonalDataModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_Disable2fa : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_Disable2fa();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<Disable2faModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public Disable2faModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<Disable2faModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_DownloadPersonalData : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_DownloadPersonalData();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<DownloadPersonalDataModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public DownloadPersonalDataModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<DownloadPersonalDataModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_EnableAuthenticator : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_EnableAuthenticator();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<EnableAuthenticatorModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public EnableAuthenticatorModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<EnableAuthenticatorModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_ExternalLogins : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_ExternalLogins();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ExternalLoginsModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ExternalLoginsModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ExternalLoginsModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_GenerateRecoveryCodes : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_GenerateRecoveryCodes();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<GenerateRecoveryCodesModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public GenerateRecoveryCodesModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<GenerateRecoveryCodesModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_Index : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_Index();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<IndexModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public IndexModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<IndexModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_PersonalData : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_PersonalData();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<PersonalDataModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public PersonalDataModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<PersonalDataModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_ResetAuthenticator : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_ResetAuthenticator();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ResetAuthenticatorModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ResetAuthenticatorModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ResetAuthenticatorModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_SetPassword : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_SetPassword();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<SetPasswordModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public SetPasswordModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<SetPasswordModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_ShowRecoveryCodes : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_ShowRecoveryCodes();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<ShowRecoveryCodesModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public ShowRecoveryCodesModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<ShowRecoveryCodesModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-    public class Areas_Identity_Pages_Account_Manage_TwoFactorAuthentication : Page {
 {
-        public Areas_Identity_Pages_Account_Manage_TwoFactorAuthentication();

-        public IViewComponentHelper Component { get; private set; }

-        public IHtmlHelper<TwoFactorAuthenticationModel> Html { get; private set; }

-        public IJsonHelper Json { get; private set; }

-        public TwoFactorAuthenticationModel Model { get; }

-        public IModelExpressionProvider ModelExpressionProvider { get; private set; }

-        public IUrlHelper Url { get; private set; }

-        public ViewDataDictionary<TwoFactorAuthenticationModel> ViewData { get; }

-        public override Task ExecuteAsync();

-    }
-}
```

