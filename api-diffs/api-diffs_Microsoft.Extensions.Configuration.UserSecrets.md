# Microsoft.Extensions.Configuration.UserSecrets

``` diff
 namespace Microsoft.Extensions.Configuration.UserSecrets {
     public class PathHelper {
         public PathHelper();
         public static string GetSecretsPathFromSecretsId(string userSecretsId);
     }
     public class UserSecretsIdAttribute : Attribute {
         public UserSecretsIdAttribute(string userSecretId);
         public string UserSecretsId { get; }
     }
 }
```

