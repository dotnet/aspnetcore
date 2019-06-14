# Microsoft.AspNetCore.JsonPatch.Internal

``` diff
-namespace Microsoft.AspNetCore.JsonPatch.Internal {
 {
-    public class ConversionResult {
 {
-        public ConversionResult(bool canBeConverted, object convertedInstance);

-        public bool CanBeConverted { get; }

-        public object ConvertedInstance { get; }

-    }
-    public static class ConversionResultProvider {
 {
-        public static ConversionResult ConvertTo(object value, Type typeToConvertTo);

-        public static ConversionResult CopyTo(object value, Type typeToConvertTo);

-    }
-    public class DictionaryAdapter<TKey, TValue> : IAdapter {
 {
-        public DictionaryAdapter();

-        public virtual bool TryAdd(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        protected virtual bool TryConvertKey(string key, out TKey convertedKey, out string errorMessage);

-        protected virtual bool TryConvertValue(object value, out TValue convertedValue, out string errorMessage);

-        public virtual bool TryGet(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        public virtual bool TryRemove(object target, string segment, IContractResolver contractResolver, out string errorMessage);

-        public virtual bool TryReplace(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTest(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object nextTarget, out string errorMessage);

-    }
-    public class DynamicObjectAdapter : IAdapter {
 {
-        public DynamicObjectAdapter();

-        public virtual bool TryAdd(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        protected virtual bool TryConvertValue(object value, Type propertyType, out object convertedValue);

-        public virtual bool TryGet(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        protected virtual bool TryGetDynamicObjectProperty(object target, IContractResolver contractResolver, string segment, out object value, out string errorMessage);

-        public virtual bool TryRemove(object target, string segment, IContractResolver contractResolver, out string errorMessage);

-        public virtual bool TryReplace(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        protected virtual bool TrySetDynamicObjectProperty(object target, IContractResolver contractResolver, string segment, object value, out string errorMessage);

-        public virtual bool TryTest(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object nextTarget, out string errorMessage);

-    }
-    public interface IAdapter {
 {
-        bool TryAdd(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        bool TryGet(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        bool TryRemove(object target, string segment, IContractResolver contractResolver, out string errorMessage);

-        bool TryReplace(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        bool TryTest(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object nextTarget, out string errorMessage);

-    }
-    public class ListAdapter : IAdapter {
 {
-        public ListAdapter();

-        public virtual bool TryAdd(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        protected virtual bool TryConvertValue(object originalValue, Type listTypeArgument, string segment, out object convertedValue, out string errorMessage);

-        public virtual bool TryGet(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        protected virtual bool TryGetListTypeArgument(IList list, out Type listTypeArgument, out string errorMessage);

-        protected virtual bool TryGetPositionInfo(IList list, string segment, ListAdapter.OperationType operationType, out ListAdapter.PositionInfo positionInfo, out string errorMessage);

-        public virtual bool TryRemove(object target, string segment, IContractResolver contractResolver, out string errorMessage);

-        public virtual bool TryReplace(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTest(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        protected enum OperationType {
 {
-            Add = 0,

-            Get = 2,

-            Remove = 1,

-            Replace = 3,

-        }
-        protected struct PositionInfo {
 {
-            public PositionInfo(ListAdapter.PositionType type, int index);

-            public int Index { get; }

-            public ListAdapter.PositionType Type { get; }

-        }
-        protected enum PositionType {
 {
-            EndOfList = 1,

-            Index = 0,

-            Invalid = 2,

-            OutOfBounds = 3,

-        }
-    }
-    public class ObjectVisitor {
 {
-        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver);

-        public ObjectVisitor(ParsedPath path, IContractResolver contractResolver, IAdapterFactory adapterFactory);

-        public bool TryVisit(ref object target, out IAdapter adapter, out string errorMessage);

-    }
-    public struct ParsedPath {
 {
-        public ParsedPath(string path);

-        public string LastSegment { get; }

-        public IReadOnlyList<string> Segments { get; }

-    }
-    public class PocoAdapter : IAdapter {
 {
-        public PocoAdapter();

-        public virtual bool TryAdd(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        protected virtual bool TryConvertValue(object value, Type propertyType, out object convertedValue);

-        public virtual bool TryGet(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-        protected virtual bool TryGetJsonProperty(object target, IContractResolver contractResolver, string segment, out JsonProperty jsonProperty);

-        public virtual bool TryRemove(object target, string segment, IContractResolver contractResolver, out string errorMessage);

-        public virtual bool TryReplace(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTest(object target, string segment, IContractResolver contractResolver, object value, out string errorMessage);

-        public virtual bool TryTraverse(object target, string segment, IContractResolver contractResolver, out object value, out string errorMessage);

-    }
-}
```

