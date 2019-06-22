# Remotion.Linq.Clauses.ResultOperators

``` diff
-namespace Remotion.Linq.Clauses.ResultOperators {
 {
-    public sealed class AggregateFromSeedResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public AggregateFromSeedResultOperator(Expression seed, LambdaExpression func, LambdaExpression optionalResultSelector);

-        public LambdaExpression Func { get; set; }

-        public LambdaExpression OptionalResultSelector { get; set; }

-        public Expression Seed { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public StreamedValue ExecuteAggregateInMemory<TInput, TAggregate, TResult>(StreamedSequence input);

-        public override StreamedValue ExecuteInMemory<TInput>(StreamedSequence input);

-        public T GetConstantSeed<T>();

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class AggregateResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public AggregateResultOperator(LambdaExpression func);

-        public LambdaExpression Func { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class AllResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public AllResultOperator(Expression predicate);

-        public Expression Predicate { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class AnyResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public AnyResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class AsQueryableResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public AsQueryableResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-        public interface ISupportedByIQueryModelVistor

-    }
-    public sealed class AverageResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public AverageResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class CastResultOperator : SequenceFromSequenceResultOperatorBase {
 {
-        public CastResultOperator(Type castItemType);

-        public Type CastItemType { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<TInput>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public abstract class ChoiceResultOperatorBase : ValueFromSequenceResultOperatorBase {
 {
-        protected ChoiceResultOperatorBase(bool returnDefaultWhenEmpty);

-        public bool ReturnDefaultWhenEmpty { get; set; }

-        public sealed override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        protected StreamedValueInfo GetOutputDataInfo(StreamedSequenceInfo inputSequenceInfo);

-    }
-    public sealed class ConcatResultOperator : SequenceFromSequenceResultOperatorBase, IQuerySource {
 {
-        public ConcatResultOperator(string itemName, Type itemType, Expression source2);

-        public string ItemName { get; set; }

-        public Type ItemType { get; set; }

-        public Expression Source2 { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public IEnumerable GetConstantSource2();

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class ContainsResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public ContainsResultOperator(Expression item);

-        public Expression Item { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public T GetConstantItem<T>();

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class CountResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public CountResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class DefaultIfEmptyResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public DefaultIfEmptyResultOperator(Expression optionalDefaultValue);

-        public Expression OptionalDefaultValue { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public object GetConstantOptionalDefaultValue();

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class DistinctResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public DistinctResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class ExceptResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public ExceptResultOperator(Expression source2);

-        public Expression Source2 { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public IEnumerable<T> GetConstantSource2<T>();

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class FirstResultOperator : ChoiceResultOperatorBase {
 {
-        public FirstResultOperator(bool returnDefaultWhenEmpty);

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class GroupResultOperator : SequenceFromSequenceResultOperatorBase, IQuerySource {
 {
-        public GroupResultOperator(string itemName, Expression keySelector, Expression elementSelector);

-        public Expression ElementSelector { get; set; }

-        public string ItemName { get; set; }

-        public Type ItemType { get; }

-        public Expression KeySelector { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public StreamedSequence ExecuteGroupingInMemory<TSource, TKey, TElement>(StreamedSequence input);

-        public override StreamedSequence ExecuteInMemory<TInput>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class IntersectResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public IntersectResultOperator(Expression source2);

-        public Expression Source2 { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public IEnumerable<T> GetConstantSource2<T>();

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class LastResultOperator : ChoiceResultOperatorBase {
 {
-        public LastResultOperator(bool returnDefaultWhenEmpty);

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class LongCountResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public LongCountResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class MaxResultOperator : ChoiceResultOperatorBase {
 {
-        public MaxResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class MinResultOperator : ChoiceResultOperatorBase {
 {
-        public MinResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class OfTypeResultOperator : SequenceFromSequenceResultOperatorBase {
 {
-        public OfTypeResultOperator(Type searchedItemType);

-        public Type SearchedItemType { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<TInput>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class ReverseResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public ReverseResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public abstract class SequenceFromSequenceResultOperatorBase : ResultOperatorBase {
 {
-        protected SequenceFromSequenceResultOperatorBase();

-        public sealed override IStreamedData ExecuteInMemory(IStreamedData input);

-        public abstract StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-    }
-    public abstract class SequenceTypePreservingResultOperatorBase : SequenceFromSequenceResultOperatorBase {
 {
-        protected SequenceTypePreservingResultOperatorBase();

-        public sealed override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        protected StreamedSequenceInfo GetOutputDataInfo(StreamedSequenceInfo inputSequenceInfo);

-    }
-    public sealed class SingleResultOperator : ChoiceResultOperatorBase {
 {
-        public SingleResultOperator(bool returnDefaultWhenEmpty);

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class SkipResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public SkipResultOperator(Expression count);

-        public Expression Count { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public int GetConstantCount();

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class SumResultOperator : ValueFromSequenceResultOperatorBase {
 {
-        public SumResultOperator();

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedValue ExecuteInMemory<T>(StreamedSequence input);

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class TakeResultOperator : SequenceTypePreservingResultOperatorBase {
 {
-        public TakeResultOperator(Expression count);

-        public Expression Count { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public int GetConstantCount();

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public sealed class UnionResultOperator : SequenceFromSequenceResultOperatorBase, IQuerySource {
 {
-        public UnionResultOperator(string itemName, Type itemType, Expression source2);

-        public string ItemName { get; set; }

-        public Type ItemType { get; set; }

-        public Expression Source2 { get; set; }

-        public override ResultOperatorBase Clone(CloneContext cloneContext);

-        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input);

-        public IEnumerable GetConstantSource2();

-        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo);

-        public override string ToString();

-        public override void TransformExpressions(Func<Expression, Expression> transformation);

-    }
-    public abstract class ValueFromSequenceResultOperatorBase : ResultOperatorBase {
 {
-        protected ValueFromSequenceResultOperatorBase();

-        public sealed override IStreamedData ExecuteInMemory(IStreamedData input);

-        public abstract StreamedValue ExecuteInMemory<T>(StreamedSequence sequence);

-    }
-}
```

