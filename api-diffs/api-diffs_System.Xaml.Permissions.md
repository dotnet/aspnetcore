# System.Xaml.Permissions

``` diff
+namespace System.Xaml.Permissions {
+    public class XamlAccessLevel {
+        public AssemblyName AssemblyAccessToAssemblyName { get; }
+        public string PrivateAccessToTypeName { get; }
+        public static XamlAccessLevel AssemblyAccessTo(Assembly assembly);
+        public static XamlAccessLevel AssemblyAccessTo(AssemblyName assemblyName);
+        public static XamlAccessLevel PrivateAccessTo(string assemblyQualifiedTypeName);
+        public static XamlAccessLevel PrivateAccessTo(Type type);
+    }
+    public sealed class XamlLoadPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public XamlLoadPermission(IEnumerable<XamlAccessLevel> allowedAccess);
+        public XamlLoadPermission(PermissionState state);
+        public XamlLoadPermission(XamlAccessLevel allowedAccess);
+        public IList<XamlAccessLevel> AllowedAccess { get; }
+        public override IPermission Copy();
+        public override bool Equals(object obj);
+        public override void FromXml(SecurityElement elem);
+        public override int GetHashCode();
+        public bool Includes(XamlAccessLevel requestedAccess);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission other);
+    }
+}
```

