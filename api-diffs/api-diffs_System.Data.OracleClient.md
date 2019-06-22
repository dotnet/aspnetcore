# System.Data.OracleClient

``` diff
 namespace System.Data.OracleClient {
     public sealed class OraclePermission : CodeAccessPermission, IUnrestrictedPermission {
         public OraclePermission(PermissionState state);
         public bool AllowBlankPassword { get; set; }
         public void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior);
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class OraclePermissionAttribute : CodeAccessSecurityAttribute {
         public OraclePermissionAttribute(SecurityAction action);
         public bool AllowBlankPassword { get; set; }
         public string ConnectionString { get; set; }
         public KeyRestrictionBehavior KeyRestrictionBehavior { get; set; }
         public string KeyRestrictions { get; set; }
         public override IPermission CreatePermission();
         public bool ShouldSerializeConnectionString();
         public bool ShouldSerializeKeyRestrictions();
     }
 }
```

