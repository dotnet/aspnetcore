## Return Task by default and ValueTask only for measured hot paths
For hot ASP.NET Core internals that often complete synchronously, return and await ValueTask without converting it to Task.

```diff
- public Task<CsrfProtectionResult> ValidateAsync(HttpContext context)
+ public async ValueTask<CsrfProtectionResult> ValidateAsync(HttpContext context)
  {
      if (HttpMethods.IsGet(context.Request.Method))
      {
-         return Task.FromResult(CsrfProtectionResult.Allowed());
+         return CsrfProtectionResult.Allowed();
      }
 
-     return ResolvePolicyAsync(context).ContinueWith(_ => CsrfProtectionResult.Allowed());
+     await ResolvePolicyAsync(context);
+     return CsrfProtectionResult.Allowed();
  }
```

## Use ConfigureAwait(false) in library internals unless context is required
In shared library code, avoid capturing a SynchronizationContext that ASP.NET Core does not require.

```diff
- var entry = await cache.GetAndRefreshAsync(key, getData: true, token);
- await cache.SetAsync(key, value, options, token);
+ var entry = await cache.GetAndRefreshAsync(key, getData: true, token).ConfigureAwait(false);
+ await cache.SetAsync(key, value, options, token).ConfigureAwait(false);
```
