# Microsoft.AspNetCore.Authentication.OAuth.Claims

``` diff
 namespace Microsoft.AspNetCore.Authentication.OAuth.Claims {
     public abstract class ClaimAction {
         public ClaimAction(string claimType, string valueType);
         public string ClaimType { get; }
         public string ValueType { get; }
-        public abstract void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public abstract void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class ClaimActionCollection : IEnumerable, IEnumerable<ClaimAction> {
         public ClaimActionCollection();
         public void Add(ClaimAction action);
         public void Clear();
         public IEnumerator<ClaimAction> GetEnumerator();
         public void Remove(string claimType);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
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
         public DeleteClaimAction(string claimType);
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class JsonKeyClaimAction : ClaimAction {
         public JsonKeyClaimAction(string claimType, string valueType, string jsonKey);
         public string JsonKey { get; }
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class JsonSubKeyClaimAction : JsonKeyClaimAction {
         public JsonSubKeyClaimAction(string claimType, string valueType, string jsonKey, string subKey);
         public string SubKey { get; }
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
     public class MapAllClaimsAction : ClaimAction {
         public MapAllClaimsAction();
-        public override void Run(JObject userData, ClaimsIdentity identity, string issuer);

+        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
     }
 }
```

