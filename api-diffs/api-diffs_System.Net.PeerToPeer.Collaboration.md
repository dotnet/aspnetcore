# System.Net.PeerToPeer.Collaboration

``` diff
 namespace System.Net.PeerToPeer.Collaboration {
     public sealed class PeerCollaborationPermission : CodeAccessPermission, IUnrestrictedPermission {
         public PeerCollaborationPermission(PermissionState state);
         public override IPermission Copy();
         public override void FromXml(SecurityElement e);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class PeerCollaborationPermissionAttribute : CodeAccessSecurityAttribute {
         public PeerCollaborationPermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
 }
```

