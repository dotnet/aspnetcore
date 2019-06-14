# Microsoft.AspNetCore.JsonPatch.Operations

``` diff
-namespace Microsoft.AspNetCore.JsonPatch.Operations {
 {
-    public class Operation : OperationBase {
 {
-        public Operation();

-        public Operation(string op, string path, string from);

-        public Operation(string op, string path, string from, object value);

-        public object value { get; set; }

-        public void Apply(object objectToApplyTo, IObjectAdapter adapter);

-        public bool ShouldSerializevalue();

-    }
-    public class Operation<TModel> : Operation where TModel : class {
 {
-        public Operation();

-        public Operation(string op, string path, string from);

-        public Operation(string op, string path, string from, object value);

-        public void Apply(TModel objectToApplyTo, IObjectAdapter adapter);

-    }
-    public class OperationBase {
 {
-        public OperationBase();

-        public OperationBase(string op, string path, string from);

-        public string from { get; set; }

-        public string op { get; set; }

-        public OperationType OperationType { get; }

-        public string path { get; set; }

-        public bool ShouldSerializefrom();

-    }
-    public enum OperationType {
 {
-        Add = 0,

-        Copy = 4,

-        Invalid = 6,

-        Move = 3,

-        Remove = 1,

-        Replace = 2,

-        Test = 5,

-    }
-}
```

