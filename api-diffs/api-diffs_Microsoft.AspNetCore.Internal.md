# Microsoft.AspNetCore.Internal

``` diff
-namespace Microsoft.AspNetCore.Internal {
 {
-    public static class AwaitableThreadPool {
 {
-        public static AwaitableThreadPool.Awaitable Yield();

-        public readonly struct Awaitable : ICriticalNotifyCompletion, INotifyCompletion {
 {
-            public bool IsCompleted { get; }

-            public AwaitableThreadPool.Awaitable GetAwaiter();

-            public void GetResult();

-            public void OnCompleted(Action continuation);

-            public void UnsafeOnCompleted(Action continuation);

-        }
-    }
-    public static class EndpointRoutingApplicationBuilderExtensions {
 {
-        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder);

-        public static IApplicationBuilder UseEndpointRouting(this IApplicationBuilder builder);

-    }
-}
```

