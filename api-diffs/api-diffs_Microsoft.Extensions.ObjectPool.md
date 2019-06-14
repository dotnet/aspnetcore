# Microsoft.Extensions.ObjectPool

``` diff
 namespace Microsoft.Extensions.ObjectPool {
+    public static class ObjectPool {
+        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy = null) where T : class, new();
+    }
 }
```

