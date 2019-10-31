// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch
{
    public partial interface IJsonPatchDocument
    {
        Newtonsoft.Json.Serialization.IContractResolver ContractResolver { get; set; }
        System.Collections.Generic.IList<Microsoft.AspNetCore.JsonPatch.Operations.Operation> GetOperations();
    }
    [Newtonsoft.Json.JsonConverterAttribute(typeof(Microsoft.AspNetCore.JsonPatch.Converters.JsonPatchDocumentConverter))]
    public partial class JsonPatchDocument : Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument
    {
        public JsonPatchDocument() { }
        public JsonPatchDocument(System.Collections.Generic.List<Microsoft.AspNetCore.JsonPatch.Operations.Operation> operations, Newtonsoft.Json.Serialization.IContractResolver contractResolver) { }
        [Newtonsoft.Json.JsonIgnoreAttribute]
        public Newtonsoft.Json.Serialization.IContractResolver ContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.JsonPatch.Operations.Operation> Operations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Add(string path, object value) { throw null; }
        public void ApplyTo(object objectToApplyTo) { }
        public void ApplyTo(object objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter) { }
        public void ApplyTo(object objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction) { }
        public void ApplyTo(object objectToApplyTo, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction) { }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Copy(string from, string path) { throw null; }
        System.Collections.Generic.IList<Microsoft.AspNetCore.JsonPatch.Operations.Operation> Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument.GetOperations() { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Move(string from, string path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Remove(string path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Replace(string path, object value) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument Test(string path, object value) { throw null; }
    }
    [Newtonsoft.Json.JsonConverterAttribute(typeof(Microsoft.AspNetCore.JsonPatch.Converters.TypedJsonPatchDocumentConverter))]
    public partial class JsonPatchDocument<TModel> : Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument where TModel : class
    {
        public JsonPatchDocument() { }
        public JsonPatchDocument(System.Collections.Generic.List<Microsoft.AspNetCore.JsonPatch.Operations.Operation<TModel>> operations, Newtonsoft.Json.Serialization.IContractResolver contractResolver) { }
        [Newtonsoft.Json.JsonIgnoreAttribute]
        public Newtonsoft.Json.Serialization.IContractResolver ContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.JsonPatch.Operations.Operation<TModel>> Operations { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Add<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Add<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value, int position) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Add<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path, TProp value) { throw null; }
        public void ApplyTo(TModel objectToApplyTo) { }
        public void ApplyTo(TModel objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter) { }
        public void ApplyTo(TModel objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction) { }
        public void ApplyTo(TModel objectToApplyTo, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction) { }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, int positionTo) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, int positionTo) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Copy<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path) { throw null; }
        System.Collections.Generic.IList<Microsoft.AspNetCore.JsonPatch.Operations.Operation> Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument.GetOperations() { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, int positionTo) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> from, int positionFrom, System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, int positionTo) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Move<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> from, System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Remove<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Remove<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, int position) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Remove<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Replace<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Replace<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value, int position) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Replace<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path, TProp value) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Test<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Test<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, System.Collections.Generic.IList<TProp>>> path, TProp value, int position) { throw null; }
        public Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> Test<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> path, TProp value) { throw null; }
    }
    public partial class JsonPatchError
    {
        public JsonPatchError(object affectedObject, Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, string errorMessage) { }
        public object AffectedObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ErrorMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.JsonPatch.Operations.Operation Operation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class JsonPatchProperty
    {
        public JsonPatchProperty(Newtonsoft.Json.Serialization.JsonProperty property, object parent) { }
        public object Parent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Newtonsoft.Json.Serialization.JsonProperty Property { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    public partial class AdapterFactory : Microsoft.AspNetCore.JsonPatch.Adapters.IAdapterFactory
    {
        public AdapterFactory() { }
        public virtual Microsoft.AspNetCore.JsonPatch.Internal.IAdapter Create(object target, Newtonsoft.Json.Serialization.IContractResolver contractResolver) { throw null; }
    }
    public partial interface IAdapterFactory
    {
        Microsoft.AspNetCore.JsonPatch.Internal.IAdapter Create(object target, Newtonsoft.Json.Serialization.IContractResolver contractResolver);
    }
    public partial interface IObjectAdapter
    {
        void Add(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
        void Copy(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
        void Move(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
        void Remove(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
        void Replace(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
    }
    public partial interface IObjectAdapterWithTest : Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter
    {
        void Test(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo);
    }
    public partial class ObjectAdapter : Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapterWithTest
    {
        public ObjectAdapter(Newtonsoft.Json.Serialization.IContractResolver contractResolver, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction) { }
        public ObjectAdapter(Newtonsoft.Json.Serialization.IContractResolver contractResolver, System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> logErrorAction, Microsoft.AspNetCore.JsonPatch.Adapters.IAdapterFactory adapterFactory) { }
        public Microsoft.AspNetCore.JsonPatch.Adapters.IAdapterFactory AdapterFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Newtonsoft.Json.Serialization.IContractResolver ContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Action<Microsoft.AspNetCore.JsonPatch.JsonPatchError> LogErrorAction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Add(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
        public void Copy(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
        public void Move(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
        public void Remove(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
        public void Replace(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
        public void Test(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo) { }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Converters
{
    public partial class JsonPatchDocumentConverter : Newtonsoft.Json.JsonConverter
    {
        public JsonPatchDocumentConverter() { }
        public override bool CanConvert(System.Type objectType) { throw null; }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer) { throw null; }
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer) { }
    }
    public partial class TypedJsonPatchDocumentConverter : Microsoft.AspNetCore.JsonPatch.Converters.JsonPatchDocumentConverter
    {
        public TypedJsonPatchDocumentConverter() { }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer) { throw null; }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Exceptions
{
    public partial class JsonPatchException : System.Exception
    {
        public JsonPatchException() { }
        public JsonPatchException(Microsoft.AspNetCore.JsonPatch.JsonPatchError jsonPatchError) { }
        public JsonPatchException(Microsoft.AspNetCore.JsonPatch.JsonPatchError jsonPatchError, System.Exception innerException) { }
        public JsonPatchException(string message, System.Exception innerException) { }
        public object AffectedObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.JsonPatch.Operations.Operation FailedOperation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Helpers
{
    public partial class GetValueResult
    {
        public GetValueResult(object propertyValue, bool hasError) { }
        public bool HasError { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object PropertyValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public partial class ConversionResult
    {
        public ConversionResult(bool canBeConverted, object convertedInstance) { }
        public bool CanBeConverted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public object ConvertedInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class ConversionResultProvider
    {
        public static Microsoft.AspNetCore.JsonPatch.Internal.ConversionResult ConvertTo(object value, System.Type typeToConvertTo) { throw null; }
        public static Microsoft.AspNetCore.JsonPatch.Internal.ConversionResult CopyTo(object value, System.Type typeToConvertTo) { throw null; }
    }
    public partial class DictionaryAdapter<TKey, TValue> : Microsoft.AspNetCore.JsonPatch.Internal.IAdapter
    {
        public DictionaryAdapter() { }
        public virtual bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        protected virtual bool TryConvertKey(string key, out TKey convertedKey, out string errorMessage) { throw null; }
        protected virtual bool TryConvertValue(object value, out TValue convertedValue, out string errorMessage) { throw null; }
        public virtual bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        public virtual bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage) { throw null; }
        public virtual bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object nextTarget, out string errorMessage) { throw null; }
    }
    public partial class DynamicObjectAdapter : Microsoft.AspNetCore.JsonPatch.Internal.IAdapter
    {
        public DynamicObjectAdapter() { }
        public virtual bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        protected virtual bool TryConvertValue(object value, System.Type propertyType, out object convertedValue) { throw null; }
        public virtual bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        protected virtual bool TryGetDynamicObjectProperty(object target, Newtonsoft.Json.Serialization.IContractResolver contractResolver, string segment, out object value, out string errorMessage) { throw null; }
        public virtual bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage) { throw null; }
        public virtual bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        protected virtual bool TrySetDynamicObjectProperty(object target, Newtonsoft.Json.Serialization.IContractResolver contractResolver, string segment, object value, out string errorMessage) { throw null; }
        public virtual bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object nextTarget, out string errorMessage) { throw null; }
    }
    public partial interface IAdapter
    {
        bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage);
        bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage);
        bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage);
        bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage);
        bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage);
        bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object nextTarget, out string errorMessage);
    }
    public partial class JObjectAdapter : Microsoft.AspNetCore.JsonPatch.Internal.IAdapter
    {
        public JObjectAdapter() { }
        public virtual bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        public virtual bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage) { throw null; }
        public virtual bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object nextTarget, out string errorMessage) { throw null; }
    }
    public partial class ListAdapter : Microsoft.AspNetCore.JsonPatch.Internal.IAdapter
    {
        public ListAdapter() { }
        public virtual bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        protected virtual bool TryConvertValue(object originalValue, System.Type listTypeArgument, string segment, out object convertedValue, out string errorMessage) { throw null; }
        public virtual bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        protected virtual bool TryGetListTypeArgument(System.Collections.IList list, out System.Type listTypeArgument, out string errorMessage) { throw null; }
        protected virtual bool TryGetPositionInfo(System.Collections.IList list, string segment, Microsoft.AspNetCore.JsonPatch.Internal.ListAdapter.OperationType operationType, out Microsoft.AspNetCore.JsonPatch.Internal.ListAdapter.PositionInfo positionInfo, out string errorMessage) { throw null; }
        public virtual bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage) { throw null; }
        public virtual bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        protected enum OperationType
        {
            Add = 0,
            Remove = 1,
            Get = 2,
            Replace = 3,
        }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        protected readonly partial struct PositionInfo
        {
            private readonly int _dummyPrimitive;
            public PositionInfo(Microsoft.AspNetCore.JsonPatch.Internal.ListAdapter.PositionType type, int index) { throw null; }
            public int Index { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public Microsoft.AspNetCore.JsonPatch.Internal.ListAdapter.PositionType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
        protected enum PositionType
        {
            Index = 0,
            EndOfList = 1,
            Invalid = 2,
            OutOfBounds = 3,
        }
    }
    public partial class ObjectVisitor
    {
        public ObjectVisitor(Microsoft.AspNetCore.JsonPatch.Internal.ParsedPath path, Newtonsoft.Json.Serialization.IContractResolver contractResolver) { }
        public ObjectVisitor(Microsoft.AspNetCore.JsonPatch.Internal.ParsedPath path, Newtonsoft.Json.Serialization.IContractResolver contractResolver, Microsoft.AspNetCore.JsonPatch.Adapters.IAdapterFactory adapterFactory) { }
        public bool TryVisit(ref object target, out Microsoft.AspNetCore.JsonPatch.Internal.IAdapter adapter, out string errorMessage) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct ParsedPath
    {
        private readonly object _dummy;
        public ParsedPath(string path) { throw null; }
        public string LastSegment { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<string> Segments { get { throw null; } }
    }
    public partial class PocoAdapter : Microsoft.AspNetCore.JsonPatch.Internal.IAdapter
    {
        public PocoAdapter() { }
        public virtual bool TryAdd(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        protected virtual bool TryConvertValue(object value, System.Type propertyType, out object convertedValue) { throw null; }
        public virtual bool TryGet(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
        protected virtual bool TryGetJsonProperty(object target, Newtonsoft.Json.Serialization.IContractResolver contractResolver, string segment, out Newtonsoft.Json.Serialization.JsonProperty jsonProperty) { throw null; }
        public virtual bool TryRemove(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out string errorMessage) { throw null; }
        public virtual bool TryReplace(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTest(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, object value, out string errorMessage) { throw null; }
        public virtual bool TryTraverse(object target, string segment, Newtonsoft.Json.Serialization.IContractResolver contractResolver, out object value, out string errorMessage) { throw null; }
    }
}
namespace Microsoft.AspNetCore.JsonPatch.Operations
{
    public partial class Operation : Microsoft.AspNetCore.JsonPatch.Operations.OperationBase
    {
        public Operation() { }
        public Operation(string op, string path, string from) { }
        public Operation(string op, string path, string from, object value) { }
        [Newtonsoft.Json.JsonPropertyAttribute("value")]
        public object value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Apply(object objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter) { }
        public bool ShouldSerializevalue() { throw null; }
    }
    public partial class OperationBase
    {
        public OperationBase() { }
        public OperationBase(string op, string path, string from) { }
        [Newtonsoft.Json.JsonPropertyAttribute("from")]
        public string from { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Newtonsoft.Json.JsonPropertyAttribute("op")]
        public string op { get { throw null; } set { } }
        [Newtonsoft.Json.JsonIgnoreAttribute]
        public Microsoft.AspNetCore.JsonPatch.Operations.OperationType OperationType { get { throw null; } }
        [Newtonsoft.Json.JsonPropertyAttribute("path")]
        public string path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShouldSerializefrom() { throw null; }
    }
    public enum OperationType
    {
        Add = 0,
        Remove = 1,
        Replace = 2,
        Move = 3,
        Copy = 4,
        Test = 5,
        Invalid = 6,
    }
    public partial class Operation<TModel> : Microsoft.AspNetCore.JsonPatch.Operations.Operation where TModel : class
    {
        public Operation() { }
        public Operation(string op, string path, string from) { }
        public Operation(string op, string path, string from, object value) { }
        public void Apply(TModel objectToApplyTo, Microsoft.AspNetCore.JsonPatch.Adapters.IObjectAdapter adapter) { }
    }
}
