# System.Data.OleDb

``` diff
 namespace System.Data.OleDb {
     public sealed class OleDbPermission : DBDataPermission {
         public OleDbPermission();
         public OleDbPermission(PermissionState state);
         public OleDbPermission(PermissionState state, bool allowBlankPassword);
         public string Provider { get; set; }
         public override IPermission Copy();
     }
     public sealed class OleDbPermissionAttribute : DBDataPermissionAttribute {
         public OleDbPermissionAttribute(SecurityAction action);
         public string Provider { get; set; }
         public override IPermission CreatePermission();
     }
 }
```

