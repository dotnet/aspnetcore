# System.Web

``` diff
+namespace System.Web {
+    public sealed class AspNetHostingPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public AspNetHostingPermission(PermissionState state);
+        public AspNetHostingPermission(AspNetHostingPermissionLevel level);
+        public AspNetHostingPermissionLevel Level { get; set; }
+        public override IPermission Copy();
+        public override void FromXml(SecurityElement securityElement);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission target);
+    }
+    public sealed class AspNetHostingPermissionAttribute : CodeAccessSecurityAttribute {
+        public AspNetHostingPermissionAttribute(SecurityAction action);
+        public AspNetHostingPermissionLevel Level { get; set; }
+        public override IPermission CreatePermission();
+    }
+    public enum AspNetHostingPermissionLevel {
+        High = 500,
+        Low = 300,
+        Medium = 400,
+        Minimal = 200,
+        None = 100,
+        Unrestricted = 600,
+    }
+}
```

