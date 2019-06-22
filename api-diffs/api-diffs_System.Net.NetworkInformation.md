# System.Net.NetworkInformation

``` diff
 namespace System.Net.NetworkInformation {
     public enum NetworkInformationAccess {
         None = 0,
         Ping = 4,
         Read = 1,
     }
     public sealed class NetworkInformationPermission : CodeAccessPermission, IUnrestrictedPermission {
         public NetworkInformationPermission(NetworkInformationAccess access);
         public NetworkInformationPermission(PermissionState state);
         public NetworkInformationAccess Access { get; }
         public void AddPermission(NetworkInformationAccess access);
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class NetworkInformationPermissionAttribute : CodeAccessSecurityAttribute {
         public NetworkInformationPermissionAttribute(SecurityAction action);
         public string Access { get; set; }
         public override IPermission CreatePermission();
     }
 }
```

