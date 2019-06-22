# System.Configuration

``` diff
+namespace System.Configuration {
+    public sealed class ConfigurationPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public ConfigurationPermission(PermissionState state);
+        public override IPermission Copy();
+        public override void FromXml(SecurityElement securityElement);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission target);
+    }
+    public sealed class ConfigurationPermissionAttribute : CodeAccessSecurityAttribute {
+        public ConfigurationPermissionAttribute(SecurityAction action);
+        public override IPermission CreatePermission();
+    }
+}
```

