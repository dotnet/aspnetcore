# System.Linq

``` diff
-namespace System.Linq {
 {
-    public static class AsyncEnumerable {
 {
-        public static Task<TResult> Aggregate<TSource, TAccumulate, TResult>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> resultSelector);

-        public static Task<TResult> Aggregate<TSource, TAccumulate, TResult>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> resultSelector, CancellationToken cancellationToken);

-        public static Task<TAccumulate> Aggregate<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator);

-        public static Task<TAccumulate> Aggregate<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, CancellationToken cancellationToken);

-        public static Task<TSource> Aggregate<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator);

-        public static Task<TSource> Aggregate<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator, CancellationToken cancellationToken);

-        public static Task<bool> All<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<bool> All<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<bool> Any<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<bool> Any<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<bool> Any<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<bool> Any<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TSource> Append<TSource>(this IAsyncEnumerable<TSource> source, TSource element);

-        public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<Decimal> Average(this IAsyncEnumerable<Decimal> source);

-        public static Task<Decimal> Average(this IAsyncEnumerable<Decimal> source, CancellationToken cancellationToken);

-        public static Task<double> Average(this IAsyncEnumerable<double> source);

-        public static Task<double> Average(this IAsyncEnumerable<double> source, CancellationToken cancellationToken);

-        public static Task<double> Average(this IAsyncEnumerable<int> source);

-        public static Task<double> Average(this IAsyncEnumerable<int> source, CancellationToken cancellationToken);

-        public static Task<double> Average(this IAsyncEnumerable<long> source);

-        public static Task<double> Average(this IAsyncEnumerable<long> source, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Average(this IAsyncEnumerable<Nullable<Decimal>> source);

-        public static Task<Nullable<Decimal>> Average(this IAsyncEnumerable<Nullable<Decimal>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<double>> source);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<double>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<int>> source);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<int>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<long>> source);

-        public static Task<Nullable<double>> Average(this IAsyncEnumerable<Nullable<long>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Average(this IAsyncEnumerable<Nullable<float>> source);

-        public static Task<Nullable<float>> Average(this IAsyncEnumerable<Nullable<float>> source, CancellationToken cancellationToken);

-        public static Task<float> Average(this IAsyncEnumerable<float> source);

-        public static Task<float> Average(this IAsyncEnumerable<float> source, CancellationToken cancellationToken);

-        public static Task<Decimal> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector);

-        public static Task<Decimal> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector, CancellationToken cancellationToken);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector);

-        public static Task<double> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector);

-        public static Task<Nullable<Decimal>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector);

-        public static Task<Nullable<double>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector);

-        public static Task<Nullable<float>> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector, CancellationToken cancellationToken);

-        public static Task<float> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector);

-        public static Task<float> Average<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<IList<TSource>> Buffer<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<IList<TSource>> Buffer<TSource>(this IAsyncEnumerable<TSource> source, int count, int skip);

-        public static IAsyncEnumerable<TResult> Cast<TResult>(this IAsyncEnumerable<object> source);

-        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, Func<TException, IAsyncEnumerable<TSource>> handler) where TException : Exception;

-        public static IAsyncEnumerable<TSource> Catch<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> Catch<TSource>(params IAsyncEnumerable<TSource>[] sources);

-        public static IAsyncEnumerable<TSource> Catch<TSource>(this IEnumerable<IAsyncEnumerable<TSource>> sources);

-        public static IAsyncEnumerable<TSource> Concat<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> Concat<TSource>(params IAsyncEnumerable<TSource>[] sources);

-        public static IAsyncEnumerable<TSource> Concat<TSource>(this IEnumerable<IAsyncEnumerable<TSource>> sources);

-        public static Task<bool> Contains<TSource>(this IAsyncEnumerable<TSource> source, TSource value);

-        public static Task<bool> Contains<TSource>(this IAsyncEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer);

-        public static Task<bool> Contains<TSource>(this IAsyncEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer, CancellationToken cancellationToken);

-        public static Task<bool> Contains<TSource>(this IAsyncEnumerable<TSource> source, TSource value, CancellationToken cancellationToken);

-        public static Task<int> Count<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<int> Count<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<int> Count<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<int> Count<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<T> CreateEnumerable<T>(Func<IAsyncEnumerator<T>> getEnumerator);

-        public static IAsyncEnumerator<T> CreateEnumerator<T>(Func<CancellationToken, Task<bool>> moveNext, Func<T> current, Action dispose);

-        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source, TSource defaultValue);

-        public static IAsyncEnumerable<TSource> Defer<TSource>(Func<IAsyncEnumerable<TSource>> factory);

-        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IAsyncEnumerable<TSource> Distinct<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer);

-        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> DistinctUntilChanged<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer);

-        public static IAsyncEnumerable<TSource> Do<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> onNext);

-        public static IAsyncEnumerable<TSource> Do<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> onNext, Action onCompleted);

-        public static IAsyncEnumerable<TSource> Do<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError);

-        public static IAsyncEnumerable<TSource> Do<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted);

-        public static IAsyncEnumerable<TSource> Do<TSource>(this IAsyncEnumerable<TSource> source, IObserver<TSource> observer);

-        public static Task<TSource> ElementAt<TSource>(this IAsyncEnumerable<TSource> source, int index);

-        public static Task<TSource> ElementAt<TSource>(this IAsyncEnumerable<TSource> source, int index, CancellationToken cancellationToken);

-        public static Task<TSource> ElementAtOrDefault<TSource>(this IAsyncEnumerable<TSource> source, int index);

-        public static Task<TSource> ElementAtOrDefault<TSource>(this IAsyncEnumerable<TSource> source, int index, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TValue> Empty<TValue>();

-        public static IAsyncEnumerable<TSource> Except<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> Except<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer);

-        public static IAsyncEnumerable<TSource> Expand<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TSource>> selector);

-        public static IAsyncEnumerable<TSource> Finally<TSource>(this IAsyncEnumerable<TSource> source, Action finallyAction);

-        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> First<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<TSource> FirstOrDefault<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> FirstOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> FirstOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> FirstOrDefault<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static void ForEach<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource, int> action);

-        public static void ForEach<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource, int> action, CancellationToken cancellationToken);

-        public static void ForEach<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action);

-        public static void ForEach<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action, CancellationToken cancellationToken);

-        public static Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource, int> action);

-        public static Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource, int> action, CancellationToken cancellationToken);

-        public static Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action);

-        public static Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TResult> Generate<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TState> iterate, Func<TState, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IAsyncEnumerable<TElement>, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IAsyncEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<IAsyncGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector);

-        public static IAsyncEnumerable<IAsyncGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IAsyncEnumerable<TSource>, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IAsyncEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<IAsyncGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IAsyncEnumerable<IAsyncGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IAsyncEnumerable<TInner>, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IAsyncEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer);

-        public static IAsyncEnumerable<TSource> IgnoreElements<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> Intersect<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> Intersect<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer);

-        public static Task<bool> IsEmpty<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<bool> IsEmpty<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IAsyncEnumerable<TOuter> outer, IAsyncEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer);

-        public static Task<TSource> Last<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> Last<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> Last<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> Last<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<TSource> LastOrDefault<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> LastOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> LastOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> LastOrDefault<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<long> LongCount<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<long> LongCount<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<long> LongCount<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<long> LongCount<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<Decimal> Max(this IAsyncEnumerable<Decimal> source);

-        public static Task<Decimal> Max(this IAsyncEnumerable<Decimal> source, CancellationToken cancellationToken);

-        public static Task<double> Max(this IAsyncEnumerable<double> source);

-        public static Task<double> Max(this IAsyncEnumerable<double> source, CancellationToken cancellationToken);

-        public static Task<int> Max(this IAsyncEnumerable<int> source);

-        public static Task<int> Max(this IAsyncEnumerable<int> source, CancellationToken cancellationToken);

-        public static Task<long> Max(this IAsyncEnumerable<long> source);

-        public static Task<long> Max(this IAsyncEnumerable<long> source, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Max(this IAsyncEnumerable<Nullable<Decimal>> source);

-        public static Task<Nullable<Decimal>> Max(this IAsyncEnumerable<Nullable<Decimal>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Max(this IAsyncEnumerable<Nullable<double>> source);

-        public static Task<Nullable<double>> Max(this IAsyncEnumerable<Nullable<double>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Max(this IAsyncEnumerable<Nullable<int>> source);

-        public static Task<Nullable<int>> Max(this IAsyncEnumerable<Nullable<int>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Max(this IAsyncEnumerable<Nullable<long>> source);

-        public static Task<Nullable<long>> Max(this IAsyncEnumerable<Nullable<long>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Max(this IAsyncEnumerable<Nullable<float>> source);

-        public static Task<Nullable<float>> Max(this IAsyncEnumerable<Nullable<float>> source, CancellationToken cancellationToken);

-        public static Task<float> Max(this IAsyncEnumerable<float> source);

-        public static Task<float> Max(this IAsyncEnumerable<float> source, CancellationToken cancellationToken);

-        public static Task<TResult> Max<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector);

-        public static Task<TResult> Max<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken);

-        public static Task<TSource> Max<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> Max<TSource>(this IAsyncEnumerable<TSource> source, IComparer<TSource> comparer);

-        public static Task<TSource> Max<TSource>(this IAsyncEnumerable<TSource> source, IComparer<TSource> comparer, CancellationToken cancellationToken);

-        public static Task<Decimal> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector);

-        public static Task<Decimal> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector, CancellationToken cancellationToken);

-        public static Task<double> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector);

-        public static Task<double> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken);

-        public static Task<int> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector);

-        public static Task<int> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken);

-        public static Task<long> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector);

-        public static Task<long> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector);

-        public static Task<Nullable<Decimal>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector);

-        public static Task<Nullable<double>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector);

-        public static Task<Nullable<int>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector);

-        public static Task<Nullable<long>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector);

-        public static Task<Nullable<float>> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector, CancellationToken cancellationToken);

-        public static Task<float> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector);

-        public static Task<float> Max<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken);

-        public static Task<TSource> Max<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken);

-        public static Task<Decimal> Min(this IAsyncEnumerable<Decimal> source);

-        public static Task<Decimal> Min(this IAsyncEnumerable<Decimal> source, CancellationToken cancellationToken);

-        public static Task<double> Min(this IAsyncEnumerable<double> source);

-        public static Task<double> Min(this IAsyncEnumerable<double> source, CancellationToken cancellationToken);

-        public static Task<int> Min(this IAsyncEnumerable<int> source);

-        public static Task<int> Min(this IAsyncEnumerable<int> source, CancellationToken cancellationToken);

-        public static Task<long> Min(this IAsyncEnumerable<long> source);

-        public static Task<long> Min(this IAsyncEnumerable<long> source, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Min(this IAsyncEnumerable<Nullable<Decimal>> source);

-        public static Task<Nullable<Decimal>> Min(this IAsyncEnumerable<Nullable<Decimal>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Min(this IAsyncEnumerable<Nullable<double>> source);

-        public static Task<Nullable<double>> Min(this IAsyncEnumerable<Nullable<double>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Min(this IAsyncEnumerable<Nullable<int>> source);

-        public static Task<Nullable<int>> Min(this IAsyncEnumerable<Nullable<int>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Min(this IAsyncEnumerable<Nullable<long>> source);

-        public static Task<Nullable<long>> Min(this IAsyncEnumerable<Nullable<long>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Min(this IAsyncEnumerable<Nullable<float>> source);

-        public static Task<Nullable<float>> Min(this IAsyncEnumerable<Nullable<float>> source, CancellationToken cancellationToken);

-        public static Task<float> Min(this IAsyncEnumerable<float> source);

-        public static Task<float> Min(this IAsyncEnumerable<float> source, CancellationToken cancellationToken);

-        public static Task<TResult> Min<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector);

-        public static Task<TResult> Min<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken cancellationToken);

-        public static Task<TSource> Min<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> Min<TSource>(this IAsyncEnumerable<TSource> source, IComparer<TSource> comparer);

-        public static Task<TSource> Min<TSource>(this IAsyncEnumerable<TSource> source, IComparer<TSource> comparer, CancellationToken cancellationToken);

-        public static Task<Decimal> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector);

-        public static Task<Decimal> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector, CancellationToken cancellationToken);

-        public static Task<double> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector);

-        public static Task<double> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken);

-        public static Task<int> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector);

-        public static Task<int> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken);

-        public static Task<long> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector);

-        public static Task<long> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector);

-        public static Task<Nullable<Decimal>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector);

-        public static Task<Nullable<double>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector);

-        public static Task<Nullable<int>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector);

-        public static Task<Nullable<long>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector);

-        public static Task<Nullable<float>> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector, CancellationToken cancellationToken);

-        public static Task<float> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector);

-        public static Task<float> Min<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken);

-        public static Task<TSource> Min<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TValue> Never<TValue>();

-        public static IAsyncEnumerable<TType> OfType<TType>(this IAsyncEnumerable<object> source);

-        public static IAsyncEnumerable<TSource> OnErrorResumeNext<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> OnErrorResumeNext<TSource>(params IAsyncEnumerable<TSource>[] sources);

-        public static IAsyncEnumerable<TSource> OnErrorResumeNext<TSource>(this IEnumerable<IAsyncEnumerable<TSource>> sources);

-        public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static IAsyncEnumerable<TSource> Prepend<TSource>(this IAsyncEnumerable<TSource> source, TSource element);

-        public static IAsyncEnumerable<int> Range(int start, int count);

-        public static IAsyncEnumerable<TResult> Repeat<TResult>(TResult element);

-        public static IAsyncEnumerable<TResult> Repeat<TResult>(TResult element, int count);

-        public static IAsyncEnumerable<TSource> Repeat<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> Repeat<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<TSource> Retry<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> Retry<TSource>(this IAsyncEnumerable<TSource> source, int retryCount);

-        public static IAsyncEnumerable<TValue> Return<TValue>(TValue value);

-        public static IAsyncEnumerable<TSource> Reverse<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TAccumulate> Scan<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator);

-        public static IAsyncEnumerable<TSource> Scan<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator);

-        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector);

-        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector);

-        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TCollection>> selector, Func<TSource, TCollection, TResult> resultSelector);

-        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TCollection>> selector, Func<TSource, TCollection, TResult> resultSelector);

-        public static IAsyncEnumerable<TOther> SelectMany<TSource, TOther>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TOther> other);

-        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector);

-        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, IAsyncEnumerable<TResult>> selector);

-        public static Task<bool> SequenceEqual<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static Task<bool> SequenceEqual<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer);

-        public static Task<bool> SequenceEqual<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer, CancellationToken cancellationToken);

-        public static Task<bool> SequenceEqual<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, CancellationToken cancellationToken);

-        public static Task<TSource> Single<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> Single<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> Single<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> Single<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<TSource> SingleOrDefault<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource> SingleOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static Task<TSource> SingleOrDefault<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken);

-        public static Task<TSource> SingleOrDefault<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TSource> Skip<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<TSource> SkipLast<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate);

-        public static IAsyncEnumerable<TSource> StartWith<TSource>(this IAsyncEnumerable<TSource> source, params TSource[] values);

-        public static Task<Decimal> Sum(this IAsyncEnumerable<Decimal> source);

-        public static Task<Decimal> Sum(this IAsyncEnumerable<Decimal> source, CancellationToken cancellationToken);

-        public static Task<double> Sum(this IAsyncEnumerable<double> source);

-        public static Task<double> Sum(this IAsyncEnumerable<double> source, CancellationToken cancellationToken);

-        public static Task<int> Sum(this IAsyncEnumerable<int> source);

-        public static Task<int> Sum(this IAsyncEnumerable<int> source, CancellationToken cancellationToken);

-        public static Task<long> Sum(this IAsyncEnumerable<long> source);

-        public static Task<long> Sum(this IAsyncEnumerable<long> source, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Sum(this IAsyncEnumerable<Nullable<Decimal>> source);

-        public static Task<Nullable<Decimal>> Sum(this IAsyncEnumerable<Nullable<Decimal>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Sum(this IAsyncEnumerable<Nullable<double>> source);

-        public static Task<Nullable<double>> Sum(this IAsyncEnumerable<Nullable<double>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Sum(this IAsyncEnumerable<Nullable<int>> source);

-        public static Task<Nullable<int>> Sum(this IAsyncEnumerable<Nullable<int>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Sum(this IAsyncEnumerable<Nullable<long>> source);

-        public static Task<Nullable<long>> Sum(this IAsyncEnumerable<Nullable<long>> source, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Sum(this IAsyncEnumerable<Nullable<float>> source);

-        public static Task<Nullable<float>> Sum(this IAsyncEnumerable<Nullable<float>> source, CancellationToken cancellationToken);

-        public static Task<float> Sum(this IAsyncEnumerable<float> source);

-        public static Task<float> Sum(this IAsyncEnumerable<float> source, CancellationToken cancellationToken);

-        public static Task<Decimal> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector);

-        public static Task<Decimal> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Decimal> selector, CancellationToken cancellationToken);

-        public static Task<double> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector);

-        public static Task<double> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, double> selector, CancellationToken cancellationToken);

-        public static Task<int> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector);

-        public static Task<int> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int> selector, CancellationToken cancellationToken);

-        public static Task<long> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector);

-        public static Task<long> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, long> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<Decimal>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector);

-        public static Task<Nullable<Decimal>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<Decimal>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<double>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector);

-        public static Task<Nullable<double>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<double>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<int>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector);

-        public static Task<Nullable<int>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<int>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<long>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector);

-        public static Task<Nullable<long>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<long>> selector, CancellationToken cancellationToken);

-        public static Task<Nullable<float>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector);

-        public static Task<Nullable<float>> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Nullable<float>> selector, CancellationToken cancellationToken);

-        public static Task<float> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector);

-        public static Task<float> Sum<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, float> selector, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TSource> Take<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<TSource> TakeLast<TSource>(this IAsyncEnumerable<TSource> source, int count);

-        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate);

-        public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer);

-        public static IAsyncEnumerable<TValue> Throw<TValue>(Exception exception);

-        public static Task<TSource[]> ToArray<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<TSource[]> ToArray<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IObservable<TSource> source);

-        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this Task<TSource> task);

-        public static Task<Dictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector);

-        public static Task<Dictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer);

-        public static Task<Dictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<Dictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken);

-        public static Task<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static Task<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer);

-        public static Task<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken);

-        public static IEnumerable<TSource> ToEnumerable<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<List<TSource>> ToList<TSource>(this IAsyncEnumerable<TSource> source);

-        public static Task<List<TSource>> ToList<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken);

-        public static Task<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector);

-        public static Task<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer);

-        public static Task<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken);

-        public static Task<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector);

-        public static Task<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer);

-        public static Task<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken);

-        public static Task<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken);

-        public static IObservable<TSource> ToObservable<TSource>(this IAsyncEnumerable<TSource> source);

-        public static IAsyncEnumerable<TSource> Union<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second);

-        public static IAsyncEnumerable<TSource> Union<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second, IEqualityComparer<TSource> comparer);

-        public static IAsyncEnumerable<TSource> Using<TSource, TResource>(Func<TResource> resourceFactory, Func<TResource, IAsyncEnumerable<TSource>> enumerableFactory) where TResource : IDisposable;

-        public static IAsyncEnumerable<TSource> Where<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate);

-        public static IAsyncEnumerable<TSource> Where<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate);

-        public static IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> selector);

-    }
-    public interface IAsyncGrouping<out TKey, out TElement> : IAsyncEnumerable<TElement> {
 {
-        TKey Key { get; }

-    }
-    public interface IOrderedAsyncEnumerable<out TElement> : IAsyncEnumerable<TElement> {
 {
-        IOrderedAsyncEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending);

-    }
-}
```

