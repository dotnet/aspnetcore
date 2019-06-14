# Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims

``` diff
-namespace Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims {
 {
-    public class UniqueJsonKeyClaimAction : JsonKeyClaimAction {
 {
-        public UniqueJsonKeyClaimAction(string claimType, string valueType, string jsonKey);

-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

-    }
-}
```

