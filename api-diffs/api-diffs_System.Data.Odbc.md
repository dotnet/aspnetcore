# System.Data.Odbc

``` diff
 namespace System.Data.Odbc {
     public sealed class OdbcPermission : DBDataPermission {
         public OdbcPermission();
         public OdbcPermission(PermissionState state);
         public OdbcPermission(PermissionState state, bool allowBlankPassword);
         public override void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior);
         public override IPermission Copy();
     }
     public sealed class OdbcPermissionAttribute : DBDataPermissionAttribute {
         public OdbcPermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
 }
```

