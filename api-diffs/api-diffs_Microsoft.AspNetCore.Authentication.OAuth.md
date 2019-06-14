# Microsoft.AspNetCore.Authentication.OAuth

``` diff
 namespace Microsoft.AspNetCore.Authentication.OAuth {
+    public class OAuthCodeExchangeContext {
+        public OAuthCodeExchangeContext(AuthenticationProperties properties, string code, string redirectUri);
+        public string Code { get; }
+        public AuthenticationProperties Properties { get; }
+        public string RedirectUri { get; }
+    }
+    public static class OAuthConstants {
+        public static readonly string CodeChallengeKey;
+        public static readonly string CodeChallengeMethodKey;
+        public static readonly string CodeChallengeMethodS256;
+        public static readonly string CodeVerifierKey;
+    }
     public class OAuthCreatingTicketContext : ResultContext<OAuthOptions> {
-        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens);

-        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens, JObject user);

+        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens, JsonElement user);
-        public JObject User { get; }
+        public JsonElement User { get; }
-        public void RunClaimActions(JObject userData);

+        public void RunClaimActions(JsonElement userData);
     }
     public class OAuthHandler<TOptions> : RemoteAuthenticationHandler<TOptions> where TOptions : OAuthOptions, new() {
+        protected virtual Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context);
-        protected virtual Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri);

     }
     public class OAuthOptions : RemoteAuthenticationOptions {
+        public bool UsePkce { get; set; }
     }
-    public class OAuthTokenResponse {
+    public class OAuthTokenResponse : IDisposable {
-        public JObject Response { get; set; }
+        public JsonDocument Response { get; set; }
+        public void Dispose();
-        public static OAuthTokenResponse Success(JObject response);

+        public static OAuthTokenResponse Success(JsonDocument response);
     }
 }
```

