# Microsoft.AspNetCore.Authentication

``` diff
 namespace Microsoft.AspNetCore.Authentication {
+    public class AccessDeniedContext : HandleRequestContext<RemoteAuthenticationOptions> {
+        public AccessDeniedContext(HttpContext context, AuthenticationScheme scheme, RemoteAuthenticationOptions options);
+        public PathString AccessDeniedPath { get; set; }
+        public AuthenticationProperties Properties { get; set; }
+        public string ReturnUrl { get; set; }
+        public string ReturnUrlParameter { get; set; }
+    }
     public class AuthenticationOptions {
+        public bool RequireAuthenticatedSignIn { get; set; }
     }
     public class AuthenticationService : IAuthenticationService {
-        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IClaimsTransformation transform);

+        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IClaimsTransformation transform, IOptions<AuthenticationOptions> options);
+        public AuthenticationOptions Options { get; }
     }
     public static class ClaimActionCollectionMapExtensions {
-        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, Func<JObject, string> resolver);

+        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, Func<JsonElement, string> resolver);
-        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, string valueType, Func<JObject, string> resolver);

+        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, string valueType, Func<JsonElement, string> resolver);
     }
-    public static class ClaimActionCollectionUniqueExtensions {
 {
-        public static void MapUniqueJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey);

-        public static void MapUniqueJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey, string valueType);

-    }
     public class HandleRequestResult : AuthenticateResult {
+        public static new HandleRequestResult NoResult();
     }
+    public static class JsonDocumentAuthExtensions {
+        public static string GetString(this JsonElement element, string key);
+    }
     public class RemoteAuthenticationEvents {
+        public Func<AccessDeniedContext, Task> OnAccessDenied { get; set; }
+        public virtual Task AccessDenied(AccessDeniedContext context);
     }
     public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationHandler, IAuthenticationRequestHandler where TOptions : RemoteAuthenticationOptions, new() {
+        protected virtual Task<HandleRequestResult> HandleAccessDeniedErrorAsync(AuthenticationProperties properties);
     }
     public class RemoteAuthenticationOptions : AuthenticationSchemeOptions {
+        public PathString AccessDeniedPath { get; set; }
+        public string ReturnUrlParameter { get; set; }
     }
 }
```

