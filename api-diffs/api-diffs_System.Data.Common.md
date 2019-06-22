# System.Data.Common

``` diff
 namespace System.Data.Common {
     public abstract class DBDataPermission : CodeAccessPermission, IUnrestrictedPermission {
         protected DBDataPermission();
-        protected DBDataPermission(DBDataPermission dataPermission);
+        protected DBDataPermission(DBDataPermission permission);
-        protected DBDataPermission(DBDataPermissionAttribute attribute);
+        protected DBDataPermission(DBDataPermissionAttribute permissionAttribute);
         protected DBDataPermission(PermissionState state);
-        protected DBDataPermission(PermissionState state, bool blankPassword);
+        protected DBDataPermission(PermissionState state, bool allowBlankPassword);
         public bool AllowBlankPassword { get; set; }
         public virtual void Add(string connectionString, string restrictions, KeyRestrictionBehavior behavior);
         protected void Clear();
         public override IPermission Copy();
         protected virtual DBDataPermission CreateInstance();
-        public override void FromXml(SecurityElement elem);
+        public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
-        public override IPermission Union(IPermission other);
+        public override IPermission Union(IPermission target);
     }
     public abstract class DBDataPermissionAttribute : CodeAccessSecurityAttribute {
         protected DBDataPermissionAttribute(SecurityAction action);
         public bool AllowBlankPassword { get; set; }
         public string ConnectionString { get; set; }
         public KeyRestrictionBehavior KeyRestrictionBehavior { get; set; }
         public string KeyRestrictions { get; set; }
         public bool ShouldSerializeConnectionString();
         public bool ShouldSerializeKeyRestrictions();
     }
 }
```

