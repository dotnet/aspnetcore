# Microsoft.Win32.SafeHandles

``` diff
+namespace Microsoft.Win32.SafeHandles {
+    public sealed class SafeAccessTokenHandle : SafeHandle {
+        public SafeAccessTokenHandle(IntPtr handle);
+        public static SafeAccessTokenHandle InvalidHandle { get; }
+        public override bool IsInvalid { get; }
+        protected override bool ReleaseHandle();
+    }
+    public abstract class SafeNCryptHandle : SafeHandleZeroOrMinusOneIsInvalid {
+        protected SafeNCryptHandle();
+        protected SafeNCryptHandle(IntPtr handle, SafeHandle parentHandle);
+        public override bool IsInvalid { get; }
+        protected override bool ReleaseHandle();
+        protected abstract bool ReleaseNativeHandle();
+    }
+    public sealed class SafeNCryptKeyHandle : SafeNCryptHandle {
+        public SafeNCryptKeyHandle();
+        public SafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle);
+        protected override bool ReleaseNativeHandle();
+    }
+    public sealed class SafeNCryptProviderHandle : SafeNCryptHandle {
+        public SafeNCryptProviderHandle();
+        protected override bool ReleaseNativeHandle();
+    }
+    public sealed class SafeNCryptSecretHandle : SafeNCryptHandle {
+        public SafeNCryptSecretHandle();
+        protected override bool ReleaseNativeHandle();
+    }
+    public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid {
+        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle);
+        public override bool IsInvalid { get; }
+        protected override bool ReleaseHandle();
+    }
+}
```

