# Microsoft.AspNetCore.JsonPatch.Exceptions

``` diff
-namespace Microsoft.AspNetCore.JsonPatch.Exceptions {
 {
-    public class JsonPatchException : Exception {
 {
-        public JsonPatchException();

-        public JsonPatchException(JsonPatchError jsonPatchError);

-        public JsonPatchException(JsonPatchError jsonPatchError, Exception innerException);

-        public JsonPatchException(string message, Exception innerException);

-        public object AffectedObject { get; private set; }

-        public Operation FailedOperation { get; private set; }

-    }
-}
```

