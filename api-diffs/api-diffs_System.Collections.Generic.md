# System.Collections.Generic

``` diff
-namespace System.Collections.Generic {
 {
-    public static class AsyncEnumerator {
 {
-        public static Task<bool> MoveNext<T>(this IAsyncEnumerator<T> enumerator);

-    }
-    public static class CollectionExtensions {
 {
-        public static IEnumerable<string> GetDefaultAssets(this IEnumerable<RuntimeAssetGroup> self);

-        public static RuntimeAssetGroup GetDefaultGroup(this IEnumerable<RuntimeAssetGroup> self);

-        public static IEnumerable<RuntimeFile> GetDefaultRuntimeFileAssets(this IEnumerable<RuntimeAssetGroup> self);

-        public static IEnumerable<string> GetRuntimeAssets(this IEnumerable<RuntimeAssetGroup> self, string runtime);

-        public static IEnumerable<RuntimeFile> GetRuntimeFileAssets(this IEnumerable<RuntimeAssetGroup> self, string runtime);

-        public static RuntimeAssetGroup GetRuntimeGroup(this IEnumerable<RuntimeAssetGroup> self, string runtime);

-    }
-    public interface IAsyncEnumerable<out T> {
 {
-        IAsyncEnumerator<T> GetEnumerator();

-    }
-    public interface IAsyncEnumerator<out T> : IDisposable {
 {
-        T Current { get; }

-        Task<bool> MoveNext(CancellationToken cancellationToken);

-    }
-}
```

