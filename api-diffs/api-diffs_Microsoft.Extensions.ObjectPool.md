# Microsoft.Extensions.ObjectPool

``` diff
 namespace Microsoft.Extensions.ObjectPool {
     public class DefaultObjectPool<T> : ObjectPool<T> where T : class {
         public DefaultObjectPool(IPooledObjectPolicy<T> policy);
         public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained);
         public override T Get();
         public override void Return(T obj);
     }
     public class DefaultObjectPoolProvider : ObjectPoolProvider {
         public DefaultObjectPoolProvider();
         public int MaximumRetained { get; set; }
         public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy);
     }
     public class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new() {
         public DefaultPooledObjectPolicy();
         public override T Create();
         public override bool Return(T obj);
     }
     public interface IPooledObjectPolicy<T> {
         T Create();
         bool Return(T obj);
     }
     public class LeakTrackingObjectPool<T> : ObjectPool<T> where T : class {
         public LeakTrackingObjectPool(ObjectPool<T> inner);
         public override T Get();
         public override void Return(T obj);
     }
     public class LeakTrackingObjectPoolProvider : ObjectPoolProvider {
         public LeakTrackingObjectPoolProvider(ObjectPoolProvider inner);
         public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy);
     }
+    public static class ObjectPool {
+        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy = null) where T : class, new();
+    }
     public abstract class ObjectPool<T> where T : class {
         protected ObjectPool();
         public abstract T Get();
         public abstract void Return(T obj);
     }
     public abstract class ObjectPoolProvider {
         protected ObjectPoolProvider();
         public ObjectPool<T> Create<T>() where T : class, new();
         public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
     }
     public static class ObjectPoolProviderExtensions {
         public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider);
         public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider, int initialCapacity, int maximumRetainedCapacity);
     }
     public abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T> {
         protected PooledObjectPolicy();
         public abstract T Create();
         public abstract bool Return(T obj);
     }
     public class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder> {
         public StringBuilderPooledObjectPolicy();
         public int InitialCapacity { get; set; }
         public int MaximumRetainedCapacity { get; set; }
         public override StringBuilder Create();
         public override bool Return(StringBuilder obj);
     }
 }
```

