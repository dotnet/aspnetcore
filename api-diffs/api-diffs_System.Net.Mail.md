# System.Net.Mail

``` diff
 namespace System.Net.Mail {
     public enum SmtpAccess {
         Connect = 1,
         ConnectToUnrestrictedPort = 2,
         None = 0,
     }
     public sealed class SmtpPermission : CodeAccessPermission, IUnrestrictedPermission {
         public SmtpPermission(bool unrestricted);
         public SmtpPermission(SmtpAccess access);
         public SmtpPermission(PermissionState state);
         public SmtpAccess Access { get; }
         public void AddPermission(SmtpAccess access);
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class SmtpPermissionAttribute : CodeAccessSecurityAttribute {
         public SmtpPermissionAttribute(SecurityAction action);
         public string Access { get; set; }
         public override IPermission CreatePermission();
     }
 }
```

