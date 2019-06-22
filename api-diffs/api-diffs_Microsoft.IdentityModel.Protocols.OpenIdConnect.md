# Microsoft.IdentityModel.Protocols.OpenIdConnect

``` diff
-namespace Microsoft.IdentityModel.Protocols.OpenIdConnect {
 {
-    public static class ActiveDirectoryOpenIdConnectEndpoints {
 {
-        public const string Authorize = "oauth2/authorize";

-        public const string Logout = "oauth2/logout";

-        public const string Token = "oauth2/token";

-    }
-    public delegate void IdTokenValidator(JwtSecurityToken idToken, OpenIdConnectProtocolValidationContext context);

-    public class OpenIdConnectConfiguration {
 {
-        public OpenIdConnectConfiguration();

-        public OpenIdConnectConfiguration(string json);

-        public ICollection<string> AcrValuesSupported { get; }

-        public virtual IDictionary<string, object> AdditionalData { get; }

-        public string AuthorizationEndpoint { get; set; }

-        public string CheckSessionIframe { get; set; }

-        public ICollection<string> ClaimsLocalesSupported { get; }

-        public bool ClaimsParameterSupported { get; set; }

-        public ICollection<string> ClaimsSupported { get; }

-        public ICollection<string> ClaimTypesSupported { get; }

-        public ICollection<string> DisplayValuesSupported { get; }

-        public string EndSessionEndpoint { get; set; }

-        public string FrontchannelLogoutSessionSupported { get; set; }

-        public string FrontchannelLogoutSupported { get; set; }

-        public ICollection<string> GrantTypesSupported { get; }

-        public bool HttpLogoutSupported { get; set; }

-        public ICollection<string> IdTokenEncryptionAlgValuesSupported { get; }

-        public ICollection<string> IdTokenEncryptionEncValuesSupported { get; }

-        public ICollection<string> IdTokenSigningAlgValuesSupported { get; }

-        public string Issuer { get; set; }

-        public JsonWebKeySet JsonWebKeySet { get; set; }

-        public string JwksUri { get; set; }

-        public bool LogoutSessionSupported { get; set; }

-        public string OpPolicyUri { get; set; }

-        public string OpTosUri { get; set; }

-        public string RegistrationEndpoint { get; set; }

-        public ICollection<string> RequestObjectEncryptionAlgValuesSupported { get; }

-        public ICollection<string> RequestObjectEncryptionEncValuesSupported { get; }

-        public ICollection<string> RequestObjectSigningAlgValuesSupported { get; }

-        public bool RequestParameterSupported { get; set; }

-        public bool RequestUriParameterSupported { get; set; }

-        public bool RequireRequestUriRegistration { get; set; }

-        public ICollection<string> ResponseModesSupported { get; }

-        public ICollection<string> ResponseTypesSupported { get; }

-        public ICollection<string> ScopesSupported { get; }

-        public string ServiceDocumentation { get; set; }

-        public ICollection<SecurityKey> SigningKeys { get; }

-        public ICollection<string> SubjectTypesSupported { get; }

-        public string TokenEndpoint { get; set; }

-        public ICollection<string> TokenEndpointAuthMethodsSupported { get; }

-        public ICollection<string> TokenEndpointAuthSigningAlgValuesSupported { get; }

-        public ICollection<string> UILocalesSupported { get; }

-        public string UserInfoEndpoint { get; set; }

-        public ICollection<string> UserInfoEndpointEncryptionAlgValuesSupported { get; }

-        public ICollection<string> UserInfoEndpointEncryptionEncValuesSupported { get; }

-        public ICollection<string> UserInfoEndpointSigningAlgValuesSupported { get; }

-        public static OpenIdConnectConfiguration Create(string json);

-        public bool ShouldSerializeAcrValuesSupported();

-        public bool ShouldSerializeClaimsLocalesSupported();

-        public bool ShouldSerializeClaimsSupported();

-        public bool ShouldSerializeClaimTypesSupported();

-        public bool ShouldSerializeDisplayValuesSupported();

-        public bool ShouldSerializeGrantTypesSupported();

-        public bool ShouldSerializeIdTokenEncryptionAlgValuesSupported();

-        public bool ShouldSerializeIdTokenEncryptionEncValuesSupported();

-        public bool ShouldSerializeIdTokenSigningAlgValuesSupported();

-        public bool ShouldSerializeRequestObjectEncryptionAlgValuesSupported();

-        public bool ShouldSerializeRequestObjectEncryptionEncValuesSupported();

-        public bool ShouldSerializeRequestObjectSigningAlgValuesSupported();

-        public bool ShouldSerializeResponseModesSupported();

-        public bool ShouldSerializeResponseTypesSupported();

-        public bool ShouldSerializeScopesSupported();

-        public bool ShouldSerializeSubjectTypesSupported();

-        public bool ShouldSerializeTokenEndpointAuthMethodsSupported();

-        public bool ShouldSerializeTokenEndpointAuthSigningAlgValuesSupported();

-        public bool ShouldSerializeUILocalesSupported();

-        public bool ShouldSerializeUserInfoEndpointEncryptionAlgValuesSupported();

-        public bool ShouldSerializeUserInfoEndpointEncryptionEncValuesSupported();

-        public bool ShouldSerializeUserInfoEndpointSigningAlgValuesSupported();

-        public static string Write(OpenIdConnectConfiguration configuration);

-    }
-    public class OpenIdConnectConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration> {
 {
-        public OpenIdConnectConfigurationRetriever();

-        public static Task<OpenIdConnectConfiguration> GetAsync(string address, IDocumentRetriever retriever, CancellationToken cancel);

-        public static Task<OpenIdConnectConfiguration> GetAsync(string address, HttpClient httpClient, CancellationToken cancel);

-        public static Task<OpenIdConnectConfiguration> GetAsync(string address, CancellationToken cancel);

-        Task<OpenIdConnectConfiguration> Microsoft.IdentityModel.Protocols.IConfigurationRetriever<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>.GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel);

-    }
-    public static class OpenIdConnectGrantTypes {
 {
-        public const string AuthorizationCode = "authorization_code";

-        public const string ClientCredentials = "client_credentials";

-        public const string Password = "password";

-        public const string RefreshToken = "refresh_token";

-    }
-    public class OpenIdConnectMessage : AuthenticationProtocolMessage {
 {
-        public OpenIdConnectMessage();

-        protected OpenIdConnectMessage(OpenIdConnectMessage other);

-        public OpenIdConnectMessage(JObject json);

-        public OpenIdConnectMessage(IEnumerable<KeyValuePair<string, string[]>> parameters);

-        public OpenIdConnectMessage(NameValueCollection nameValueCollection);

-        public OpenIdConnectMessage(string json);

-        public string AccessToken { get; set; }

-        public string AcrValues { get; set; }

-        public string AuthorizationEndpoint { get; set; }

-        public string ClaimsLocales { get; set; }

-        public string ClientAssertion { get; set; }

-        public string ClientAssertionType { get; set; }

-        public string ClientId { get; set; }

-        public string ClientSecret { get; set; }

-        public string Code { get; set; }

-        public string Display { get; set; }

-        public string DomainHint { get; set; }

-        public bool EnableTelemetryParameters { get; set; }

-        public static bool EnableTelemetryParametersByDefault { get; set; }

-        public string Error { get; set; }

-        public string ErrorDescription { get; set; }

-        public string ErrorUri { get; set; }

-        public string ExpiresIn { get; set; }

-        public string GrantType { get; set; }

-        public string IdentityProvider { get; set; }

-        public string IdToken { get; set; }

-        public string IdTokenHint { get; set; }

-        public string Iss { get; set; }

-        public string LoginHint { get; set; }

-        public string MaxAge { get; set; }

-        public string Nonce { get; set; }

-        public string Password { get; set; }

-        public string PostLogoutRedirectUri { get; set; }

-        public string Prompt { get; set; }

-        public string RedirectUri { get; set; }

-        public string RefreshToken { get; set; }

-        public OpenIdConnectRequestType RequestType { get; set; }

-        public string RequestUri { get; set; }

-        public string Resource { get; set; }

-        public string ResponseMode { get; set; }

-        public string ResponseType { get; set; }

-        public string Scope { get; set; }

-        public string SessionState { get; set; }

-        public string Sid { get; set; }

-        public string SkuTelemetryValue { get; set; }

-        public string State { get; set; }

-        public string TargetLinkUri { get; set; }

-        public string TokenEndpoint { get; set; }

-        public string TokenType { get; set; }

-        public string UiLocales { get; set; }

-        public string UserId { get; set; }

-        public string Username { get; set; }

-        public virtual OpenIdConnectMessage Clone();

-        public virtual string CreateAuthenticationRequestUrl();

-        public virtual string CreateLogoutRequestUrl();

-    }
-    public static class OpenIdConnectParameterNames {
 {
-        public const string AccessToken = "access_token";

-        public const string AcrValues = "acr_values";

-        public const string ClaimsLocales = "claims_locales";

-        public const string ClientAssertion = "client_assertion";

-        public const string ClientAssertionType = "client_assertion_type";

-        public const string ClientId = "client_id";

-        public const string ClientSecret = "client_secret";

-        public const string Code = "code";

-        public const string Display = "display";

-        public const string DomainHint = "domain_hint";

-        public const string Error = "error";

-        public const string ErrorDescription = "error_description";

-        public const string ErrorUri = "error_uri";

-        public const string ExpiresIn = "expires_in";

-        public const string GrantType = "grant_type";

-        public const string IdentityProvider = "identity_provider";

-        public const string IdToken = "id_token";

-        public const string IdTokenHint = "id_token_hint";

-        public const string Iss = "iss";

-        public const string LoginHint = "login_hint";

-        public const string MaxAge = "max_age";

-        public const string Nonce = "nonce";

-        public const string Password = "password";

-        public const string PostLogoutRedirectUri = "post_logout_redirect_uri";

-        public const string Prompt = "prompt";

-        public const string RedirectUri = "redirect_uri";

-        public const string RefreshToken = "refresh_token";

-        public const string RequestUri = "request_uri";

-        public const string Resource = "resource";

-        public const string ResponseMode = "response_mode";

-        public const string ResponseType = "response_type";

-        public const string Scope = "scope";

-        public const string SessionState = "session_state";

-        public const string Sid = "sid";

-        public const string SkuTelemetry = "x-client-SKU";

-        public const string State = "state";

-        public const string TargetLinkUri = "target_link_uri";

-        public const string TokenType = "token_type";

-        public const string UiLocales = "ui_locales";

-        public const string UserId = "user_id";

-        public const string Username = "username";

-        public const string VersionTelemetry = "x-client-ver";

-    }
-    public static class OpenIdConnectPrompt {
 {
-        public const string Consent = "consent";

-        public const string Login = "login";

-        public const string None = "none";

-        public const string SelectAccount = "select_account";

-    }
-    public class OpenIdConnectProtocolException : Exception {
 {
-        public OpenIdConnectProtocolException();

-        public OpenIdConnectProtocolException(string message);

-        public OpenIdConnectProtocolException(string message, Exception innerException);

-    }
-    public class OpenIdConnectProtocolInvalidAtHashException : OpenIdConnectProtocolException {
 {
-        public OpenIdConnectProtocolInvalidAtHashException();

-        public OpenIdConnectProtocolInvalidAtHashException(string message);

-        public OpenIdConnectProtocolInvalidAtHashException(string message, Exception innerException);

-    }
-    public class OpenIdConnectProtocolInvalidCHashException : OpenIdConnectProtocolException {
 {
-        public OpenIdConnectProtocolInvalidCHashException();

-        public OpenIdConnectProtocolInvalidCHashException(string message);

-        public OpenIdConnectProtocolInvalidCHashException(string message, Exception innerException);

-    }
-    public class OpenIdConnectProtocolInvalidNonceException : OpenIdConnectProtocolException {
 {
-        public OpenIdConnectProtocolInvalidNonceException();

-        public OpenIdConnectProtocolInvalidNonceException(string message);

-        public OpenIdConnectProtocolInvalidNonceException(string message, Exception innerException);

-    }
-    public class OpenIdConnectProtocolInvalidStateException : OpenIdConnectProtocolException {
 {
-        public OpenIdConnectProtocolInvalidStateException();

-        public OpenIdConnectProtocolInvalidStateException(string message);

-        public OpenIdConnectProtocolInvalidStateException(string message, Exception innerException);

-    }
-    public class OpenIdConnectProtocolValidationContext {
 {
-        public OpenIdConnectProtocolValidationContext();

-        public string ClientId { get; set; }

-        public string Nonce { get; set; }

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public string State { get; set; }

-        public string UserInfoEndpointResponse { get; set; }

-        public JwtSecurityToken ValidatedIdToken { get; set; }

-    }
-    public class OpenIdConnectProtocolValidator {
 {
-        public static readonly TimeSpan DefaultNonceLifetime;

-        public OpenIdConnectProtocolValidator();

-        public CryptoProviderFactory CryptoProviderFactory { get; set; }

-        public IDictionary<string, string> HashAlgorithmMap { get; }

-        public IdTokenValidator IdTokenValidator { get; set; }

-        public TimeSpan NonceLifetime { get; set; }

-        public bool RequireAcr { get; set; }

-        public bool RequireAmr { get; set; }

-        public bool RequireAuthTime { get; set; }

-        public bool RequireAzp { get; set; }

-        public bool RequireNonce { get; set; }

-        public bool RequireState { get; set; }

-        public bool RequireStateValidation { get; set; }

-        public bool RequireSub { get; set; }

-        public static bool RequireSubByDefault { get; set; }

-        public bool RequireTimeStampInNonce { get; set; }

-        public virtual string GenerateNonce();

-        public virtual HashAlgorithm GetHashAlgorithm(string algorithm);

-        protected virtual void ValidateAtHash(OpenIdConnectProtocolValidationContext validationContext);

-        public virtual void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext);

-        protected virtual void ValidateCHash(OpenIdConnectProtocolValidationContext validationContext);

-        protected virtual void ValidateIdToken(OpenIdConnectProtocolValidationContext validationContext);

-        protected virtual void ValidateNonce(OpenIdConnectProtocolValidationContext validationContext);

-        protected virtual void ValidateState(OpenIdConnectProtocolValidationContext validationContext);

-        public virtual void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext);

-        public virtual void ValidateUserInfoResponse(OpenIdConnectProtocolValidationContext validationContext);

-    }
-    public enum OpenIdConnectRequestType {
 {
-        Authentication = 0,

-        Logout = 1,

-        Token = 2,

-    }
-    public static class OpenIdConnectResponseMode {
 {
-        public const string FormPost = "form_post";

-        public const string Fragment = "fragment";

-        public const string Query = "query";

-    }
-    public static class OpenIdConnectResponseType {
 {
-        public const string Code = "code";

-        public const string CodeIdToken = "code id_token";

-        public const string CodeIdTokenToken = "code id_token token";

-        public const string CodeToken = "code token";

-        public const string IdToken = "id_token";

-        public const string IdTokenToken = "id_token token";

-        public const string None = "none";

-        public const string Token = "token";

-    }
-    public static class OpenIdConnectScope {
 {
-        public const string Email = "email";

-        public const string OfflineAccess = "offline_access";

-        public const string OpenId = "openid";

-        public const string OpenIdProfile = "openid profile";

-        public const string UserImpersonation = "user_impersonation";

-    }
-    public static class OpenIdConnectSessionProperties {
 {
-        public const string CheckSessionIFrame = ".checkSessionIFrame";

-        public const string RedirectUri = ".redirect_uri";

-        public const string SessionState = ".sessionState";

-    }
-    public static class OpenIdProviderMetadataNames {
 {
-        public const string AcrValuesSupported = "acr_values_supported";

-        public const string AuthorizationEndpoint = "authorization_endpoint";

-        public const string CheckSessionIframe = "check_session_iframe";

-        public const string ClaimsLocalesSupported = "claims_locales_supported";

-        public const string ClaimsParameterSupported = "claims_parameter_supported";

-        public const string ClaimsSupported = "claims_supported";

-        public const string ClaimTypesSupported = "claim_types_supported";

-        public const string Discovery = ".well-known/openid-configuration";

-        public const string DisplayValuesSupported = "display_values_supported";

-        public const string EndSessionEndpoint = "end_session_endpoint";

-        public const string FrontchannelLogoutSessionSupported = "frontchannel_logout_session_supported";

-        public const string FrontchannelLogoutSupported = "frontchannel_logout_supported";

-        public const string GrantTypesSupported = "grant_types_supported";

-        public const string HttpLogoutSupported = "http_logout_supported";

-        public const string IdTokenEncryptionAlgValuesSupported = "id_token_encryption_alg_values_supported";

-        public const string IdTokenEncryptionEncValuesSupported = "id_token_encryption_enc_values_supported";

-        public const string IdTokenSigningAlgValuesSupported = "id_token_signing_alg_values_supported";

-        public const string Issuer = "issuer";

-        public const string JwksUri = "jwks_uri";

-        public const string LogoutSessionSupported = "logout_session_supported";

-        public const string MicrosoftMultiRefreshToken = "microsoft_multi_refresh_token";

-        public const string OpPolicyUri = "op_policy_uri";

-        public const string OpTosUri = "op_tos_uri";

-        public const string RegistrationEndpoint = "registration_endpoint";

-        public const string RequestObjectEncryptionAlgValuesSupported = "request_object_encryption_alg_values_supported";

-        public const string RequestObjectEncryptionEncValuesSupported = "request_object_encryption_enc_values_supported";

-        public const string RequestObjectSigningAlgValuesSupported = "request_object_signing_alg_values_supported";

-        public const string RequestParameterSupported = "request_parameter_supported";

-        public const string RequestUriParameterSupported = "request_uri_parameter_supported";

-        public const string RequireRequestUriRegistration = "require_request_uri_registration";

-        public const string ResponseModesSupported = "response_modes_supported";

-        public const string ResponseTypesSupported = "response_types_supported";

-        public const string ScopesSupported = "scopes_supported";

-        public const string ServiceDocumentation = "service_documentation";

-        public const string SubjectTypesSupported = "subject_types_supported";

-        public const string TokenEndpoint = "token_endpoint";

-        public const string TokenEndpointAuthMethodsSupported = "token_endpoint_auth_methods_supported";

-        public const string TokenEndpointAuthSigningAlgValuesSupported = "token_endpoint_auth_signing_alg_values_supported";

-        public const string UILocalesSupported = "ui_locales_supported";

-        public const string UserInfoEncryptionAlgValuesSupported = "userinfo_encryption_alg_values_supported";

-        public const string UserInfoEncryptionEncValuesSupported = "userinfo_encryption_enc_values_supported";

-        public const string UserInfoEndpoint = "userinfo_endpoint";

-        public const string UserInfoSigningAlgValuesSupported = "userinfo_signing_alg_values_supported";

-    }
-}
```

