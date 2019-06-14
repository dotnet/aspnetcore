# Microsoft.AspNetCore.JsonPatch.Adapters

``` diff
-namespace Microsoft.AspNetCore.JsonPatch.Adapters {
 {
-    public class AdapterFactory : IAdapterFactory {
 {
-        public AdapterFactory();

-        public virtual IAdapter Create(object target, IContractResolver contractResolver);

-    }
-    public interface IAdapterFactory {
 {
-        IAdapter Create(object target, IContractResolver contractResolver);

-    }
-    public interface IObjectAdapter {
 {
-        void Add(Operation operation, object objectToApplyTo);

-        void Copy(Operation operation, object objectToApplyTo);

-        void Move(Operation operation, object objectToApplyTo);

-        void Remove(Operation operation, object objectToApplyTo);

-        void Replace(Operation operation, object objectToApplyTo);

-    }
-    public interface IObjectAdapterWithTest : IObjectAdapter {
 {
-        void Test(Operation operation, object objectToApplyTo);

-    }
-    public class ObjectAdapter : IObjectAdapter, IObjectAdapterWithTest {
 {
-        public ObjectAdapter(IContractResolver contractResolver, Action<JsonPatchError> logErrorAction);

-        public ObjectAdapter(IContractResolver contractResolver, Action<JsonPatchError> logErrorAction, IAdapterFactory adapterFactory);

-        public IAdapterFactory AdapterFactory { get; }

-        public IContractResolver ContractResolver { get; }

-        public Action<JsonPatchError> LogErrorAction { get; }

-        public void Add(Operation operation, object objectToApplyTo);

-        public void Copy(Operation operation, object objectToApplyTo);

-        public void Move(Operation operation, object objectToApplyTo);

-        public void Remove(Operation operation, object objectToApplyTo);

-        public void Replace(Operation operation, object objectToApplyTo);

-        public void Test(Operation operation, object objectToApplyTo);

-    }
-}
```

