# Microsoft.Extensions.Logging.Abstractions.Internal

``` diff
-namespace Microsoft.Extensions.Logging.Abstractions.Internal {
 {
-    public class NullScope : IDisposable {
 {
-        public static NullScope Instance { get; }

-        public void Dispose();

-    }
-    public class TypeNameHelper {
 {
-        public TypeNameHelper();

-        public static string GetTypeDisplayName(Type type);

-    }
-}
```

