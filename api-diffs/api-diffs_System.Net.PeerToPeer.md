# System.Net.PeerToPeer

``` diff
 namespace System.Net.PeerToPeer {
     public sealed class PnrpPermission : CodeAccessPermission, IUnrestrictedPermission {
         public PnrpPermission(PermissionState state);
         public override IPermission Copy();
         public override void FromXml(SecurityElement e);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class PnrpPermissionAttribute : CodeAccessSecurityAttribute {
         public PnrpPermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
     public enum PnrpScope {
         All = 0,
         Global = 1,
         LinkLocal = 3,
         SiteLocal = 2,
     }
 }
```

