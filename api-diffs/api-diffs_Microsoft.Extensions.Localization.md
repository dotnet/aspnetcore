# Microsoft.Extensions.Localization

``` diff
 namespace Microsoft.Extensions.Localization {
-    public interface IStringLocalizer<T> : IStringLocalizer
+    public interface IStringLocalizer<out T> : IStringLocalizer
 }
```

