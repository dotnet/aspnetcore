# Microsoft.Extensions.DiagnosticAdapter.Internal

``` diff
-namespace Microsoft.Extensions.DiagnosticAdapter.Internal {
 {
-    public class InvalidProxyOperationException : InvalidOperationException {
 {
-        public InvalidProxyOperationException(string message);

-    }
-    public static class ProxyAssembly {
 {
-        public static TypeBuilder DefineType(string name, TypeAttributes attributes, Type baseType, Type[] interfaces);

-    }
-    public abstract class ProxyBase : IProxy {
 {
-        public readonly Type WrappedType;

-        protected ProxyBase(Type wrappedType);

-        public abstract object UnderlyingInstanceAsObject { get; }

-        public T Upwrap<T>();

-    }
-    public class ProxyBase<T> : ProxyBase where T : class {
 {
-        public readonly T Instance;

-        public ProxyBase(T instance);

-        public T UnderlyingInstance { get; }

-        public override object UnderlyingInstanceAsObject { get; }

-    }
-    public class ProxyEnumerable<TSourceElement, TTargetElement> : IEnumerable, IEnumerable<TTargetElement> {
 {
-        public ProxyEnumerable(IEnumerable<TSourceElement> source, Type proxyType);

-        public IEnumerator<TTargetElement> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public class ProxyEnumerator : IDisposable, IEnumerator, IEnumerator<TTargetElement> {
 {
-            public ProxyEnumerator(IEnumerator<TSourceElement> source, Type proxyType);

-            public TTargetElement Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public class ProxyFactory : IProxyFactory {
 {
-        public ProxyFactory();

-        public TProxy CreateProxy<TProxy>(object obj);

-    }
-    public class ProxyList<TSourceElement, TTargetElement> : IEnumerable, IEnumerable<TTargetElement>, IReadOnlyCollection<TTargetElement>, IReadOnlyList<TTargetElement> {
 {
-        public ProxyList(IList<TSourceElement> source);

-        protected ProxyList(IList<TSourceElement> source, Type proxyType);

-        public int Count { get; }

-        public TTargetElement this[int index] { get; }

-        public IEnumerator<TTargetElement> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public static class ProxyMethodEmitter {
 {
-        public static Func<object, object, IProxyFactory, bool> CreateProxyMethod(MethodInfo method, Type inputType);

-    }
-    public class ProxyTypeCache : ConcurrentDictionary<Tuple<Type, Type>, ProxyTypeCacheResult> {
 {
-        public ProxyTypeCache();

-    }
-    public class ProxyTypeCacheResult {
 {
-        public ProxyTypeCacheResult();

-        public ConstructorInfo Constructor { get; private set; }

-        public string Error { get; private set; }

-        public bool IsError { get; }

-        public Tuple<Type, Type> Key { get; private set; }

-        public Type Type { get; private set; }

-        public static ProxyTypeCacheResult FromError(Tuple<Type, Type> key, string error);

-        public static ProxyTypeCacheResult FromType(Tuple<Type, Type> key, Type type, ConstructorInfo constructor);

-    }
-    public static class ProxyTypeEmitter {
 {
-        public static Type GetProxyType(ProxyTypeCache cache, Type targetType, Type sourceType);

-    }
-}
```

