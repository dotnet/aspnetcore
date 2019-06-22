# System.Drawing.Printing

``` diff
 namespace System.Drawing.Printing {
     public sealed class PrintingPermission : CodeAccessPermission, IUnrestrictedPermission {
         public PrintingPermission(PrintingPermissionLevel printingLevel);
         public PrintingPermission(PermissionState state);
         public PrintingPermissionLevel Level { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement element);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class PrintingPermissionAttribute : CodeAccessSecurityAttribute {
         public PrintingPermissionAttribute(SecurityAction action);
         public PrintingPermissionLevel Level { get; set; }
         public override IPermission CreatePermission();
     }
     public enum PrintingPermissionLevel {
         AllPrinting = 3,
         DefaultPrinting = 2,
         NoPrinting = 0,
         SafePrinting = 1,
     }
 }
```

