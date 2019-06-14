# Microsoft.AspNetCore.Authentication.OAuth.Claims

``` diff
 namespace Microsoft.AspNetCore.Authentication.OAuth.Claims {
     public abstract class ClaimAction {
-        public abstract void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public abstract void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class CustomJsonClaimAction : ClaimAction {
-        public CustomJsonClaimAction(string claimType, string valueType, Func<JObject, string> resolver);

+        public CustomJsonClaimAction(string claimType, string valueType, Func<JsonElement, string> resolver);
-        public Func<JObject, string> Resolver { get; }
+        public Func<JsonElement, string> Resolver { get; }
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class DeleteClaimAction : ClaimAction {
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class JsonKeyClaimAction : ClaimAction {
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class JsonSubKeyClaimAction : JsonKeyClaimAction {
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class MapAllClaimsAction : ClaimAction {
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
 }
```

