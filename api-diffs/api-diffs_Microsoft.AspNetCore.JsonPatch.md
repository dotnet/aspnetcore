# Microsoft.AspNetCore.JsonPatch

``` diff
-namespace Microsoft.AspNetCore.JsonPatch {
 {
-    public interface IJsonPatchDocument {
 {
-        IContractResolver ContractResolver { get; set; }

-        IList<Operation> GetOperations();

-    }
-    public class JsonPatchDocument : IJsonPatchDocument {
 {
-        public JsonPatchDocument();

-        public JsonPatchDocument(List<Operation> operations, IContractResolver contractResolver);

-        public IContractResolver ContractResolver { get; set; }

-        public List<Operation> Operations { get; private set; }

-        public JsonPatchDocument Add(string path, object value);

-        public void ApplyTo(object objectToApplyTo);

-        public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter);

-        public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter, Action<JsonPatchError> logErrorAction);

-        public void ApplyTo(object objectToApplyTo, Action<JsonPatchError> logErrorAction);

-        public JsonPatchDocument Copy(string from, string path);

-        IList<Operation> Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument.GetOperations();

-        public JsonPatchDocument Move(string from, string path);

-        public JsonPatchDocument Remove(string path);

-        public JsonPatchDocument Replace(string path, object value);

-        public JsonPatchDocument Test(string path, object value);

-    }
-    public class JsonPatchDocument<TModel> : IJsonPatchDocument where TModel : class {
 {
-        public JsonPatchDocument();

-        public JsonPatchDocument(List<Operation<TModel>> operations, IContractResolver contractResolver);

-        public IContractResolver ContractResolver { get; set; }

-        public List<Operation<TModel>> Operations { get; private set; }

-        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value);

-        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position);

-        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, TProp>> path, TProp value);

-        public void ApplyTo(TModel objectToApplyTo);

-        public void ApplyTo(TModel objectToApplyTo, IObjectAdapter adapter);

-        public void ApplyTo(TModel objectToApplyTo, IObjectAdapter adapter, Action<JsonPatchError> logErrorAction);

-        public void ApplyTo(TModel objectToApplyTo, Action<JsonPatchError> logErrorAction);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path, int positionTo);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, TProp>> path);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path, int positionTo);

-        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, TProp>> path);

-        IList<Operation> Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument.GetOperations();

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path);

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path, int positionTo);

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, TProp>> path);

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path);

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path, int positionTo);

-        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, TProp>> path);

-        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path);

-        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path, int position);

-        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, TProp>> path);

-        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value);

-        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position);

-        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, TProp>> path, TProp value);

-        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value);

-        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position);

-        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, TProp>> path, TProp value);

-    }
-    public class JsonPatchError {
 {
-        public JsonPatchError(object affectedObject, Operation operation, string errorMessage);

-        public object AffectedObject { get; }

-        public string ErrorMessage { get; }

-        public Operation Operation { get; }

-    }
-    public class JsonPatchProperty {
 {
-        public JsonPatchProperty(JsonProperty property, object parent);

-        public object Parent { get; set; }

-        public JsonProperty Property { get; set; }

-    }
-}
```

