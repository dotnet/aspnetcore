# Remotion.Linq.Clauses.StreamedData

``` diff
-namespace Remotion.Linq.Clauses.StreamedData {
 {
-    public interface IStreamedData {
 {
-        IStreamedDataInfo DataInfo { get; }

-        object Value { get; }

-    }
-    public interface IStreamedDataInfo : IEquatable<IStreamedDataInfo> {
 {
-        Type DataType { get; }

-        IStreamedDataInfo AdjustDataType(Type dataType);

-        IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor);

-    }
-    public sealed class StreamedScalarValueInfo : StreamedValueInfo {
 {
-        public StreamedScalarValueInfo(Type dataType);

-        protected override StreamedValueInfo CloneWithNewDataType(Type dataType);

-        public override IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor);

-        public object ExecuteScalarQueryModel<T>(QueryModel queryModel, IQueryExecutor executor);

-    }
-    public sealed class StreamedSequence : IStreamedData {
 {
-        public StreamedSequence(IEnumerable sequence, StreamedSequenceInfo streamedSequenceInfo);

-        public StreamedSequenceInfo DataInfo { get; private set; }

-        IStreamedDataInfo Remotion.Linq.Clauses.StreamedData.IStreamedData.DataInfo { get; }

-        object Remotion.Linq.Clauses.StreamedData.IStreamedData.Value { get; }

-        public IEnumerable Sequence { get; private set; }

-        public IEnumerable<T> GetTypedSequence<T>();

-    }
-    public sealed class StreamedSequenceInfo : IEquatable<IStreamedDataInfo>, IStreamedDataInfo {
 {
-        public StreamedSequenceInfo(Type dataType, Expression itemExpression);

-        public Type DataType { get; private set; }

-        public Expression ItemExpression { get; private set; }

-        public Type ResultItemType { get; private set; }

-        public IStreamedDataInfo AdjustDataType(Type dataType);

-        public bool Equals(IStreamedDataInfo obj);

-        public sealed override bool Equals(object obj);

-        public IEnumerable ExecuteCollectionQueryModel<T>(QueryModel queryModel, IQueryExecutor executor);

-        public IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor);

-        public override int GetHashCode();

-    }
-    public sealed class StreamedSingleValueInfo : StreamedValueInfo {
 {
-        public StreamedSingleValueInfo(Type dataType, bool returnDefaultWhenEmpty);

-        public bool ReturnDefaultWhenEmpty { get; }

-        protected override StreamedValueInfo CloneWithNewDataType(Type dataType);

-        public override bool Equals(IStreamedDataInfo obj);

-        public override IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor);

-        public object ExecuteSingleQueryModel<T>(QueryModel queryModel, IQueryExecutor executor);

-        public override int GetHashCode();

-    }
-    public sealed class StreamedValue : IStreamedData {
 {
-        public StreamedValue(object value, StreamedValueInfo streamedValueInfo);

-        public StreamedValueInfo DataInfo { get; private set; }

-        IStreamedDataInfo Remotion.Linq.Clauses.StreamedData.IStreamedData.DataInfo { get; }

-        public object Value { get; private set; }

-        public T GetTypedValue<T>();

-    }
-    public abstract class StreamedValueInfo : IEquatable<IStreamedDataInfo>, IStreamedDataInfo {
 {
-        public Type DataType { get; private set; }

-        public virtual IStreamedDataInfo AdjustDataType(Type dataType);

-        protected abstract StreamedValueInfo CloneWithNewDataType(Type dataType);

-        public virtual bool Equals(IStreamedDataInfo obj);

-        public sealed override bool Equals(object obj);

-        public abstract IStreamedData ExecuteQueryModel(QueryModel queryModel, IQueryExecutor executor);

-        public override int GetHashCode();

-    }
-}
```

