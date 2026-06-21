# Collections performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid enumerator boxing by preserving concrete collection types

Keep hot foreach loops typed as concrete collections or spans rather than IEnumerable<T> when possible.

- Do: Iterate List<T>, T[], Queue<T>, Stack<T>, HashSet<T>, or spans directly; use CollectionsMarshal.AsSpan for List<T> when safe.
- Instead of: Store hot-loop inputs as IEnumerable<T> and then foreach repeatedly.
- Why: Concrete collection enumeration uses struct enumerators and avoids boxed IEnumerator<T>, interface dispatch, and heap allocations.
- Since .NET 10. Supersedes: .NET 8 empty enumerator singletons and .NET 10 JIT/enumerator changes reduce boxing automatically, but concrete typing remains the safest author-controlled pattern.
- Hot path: yes | Complexity: low
- APIs: `System.Collections.Generic.IEnumerable<T>`, `System.Runtime.InteropServices.CollectionsMarshal.AsSpan`

## Use CollectionsMarshal.AsSpan for controlled List access

Use CollectionsMarshal.AsSpan to read or fill a List<T> through its backing storage when you fully control mutations.

- Do: Use CollectionsMarshal.SetCount to establish the final count, then CollectionsMarshal.AsSpan(list) to fill or process the list.
- Instead of: Repeated List<T>.Add in inner loops when the final size is known and direct filling is safe.
- Why: Direct span access removes enumerator, interface, Add, and bounds-check overhead in tight code.
- Since .NET 8. Supersedes: .NET 5 exposed AsSpan for the current Count; .NET 8 added SetCount to enable efficient writing into new list space.
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.InteropServices.CollectionsMarshal.AsSpan`, `System.Runtime.InteropServices.CollectionsMarshal.SetCount`
- Snippet: [code](../snippets/bcl/collections.md#use-collectionsmarshalasspan-for-controlled-list-access)

## Use alternate-key lookups for string dictionaries

Use GetAlternateLookup or TryGetAlternateLookup when probing string-keyed collections with ReadOnlySpan<char> or another alternate representation.

- Do: Use Dictionary<TKey,TValue>.GetAlternateLookup<TAlternateKey>() with a comparer implementing IAlternateEqualityComparer<TAlternate,TKey>.
- Instead of: Call span.ToString() before TryGetValue, ContainsKey, or the indexer.
- Why: It avoids allocating temporary strings for lookups and creates a real key only when insertion is needed.
- Since .NET 9. Supersedes: Manual custom key wrappers or allocating strings for span lookups before .NET 9.
- Hot path: yes | Complexity: medium
- APIs: `System.Collections.Generic.Dictionary<TKey,TValue>.GetAlternateLookup`, `System.Collections.Generic.Dictionary<TKey,TValue>.TryGetAlternateLookup`, `System.Collections.Generic.IAlternateEqualityComparer<TAlternate,T>`, `System.StringComparer`

## Use ref-return dictionary helpers for update-in-place

Use CollectionsMarshal ref-return helpers when a hot path updates a dictionary value or adds a default value in one probe.

- Do: Use CollectionsMarshal.GetValueRefOrAddDefault or GetValueRefOrNullRef, including the .NET 9 AlternateLookup overload.
- Instead of: TryGetValue followed by indexer assignment, or ContainsKey followed by another lookup.
- Why: They avoid duplicate lookups and can avoid copying large struct values.
- Since .NET 9. Supersedes: .NET 6 added key-based ref helpers; .NET 9 extends them to alternate-key lookups.
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault`, `System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef`, `System.Collections.Generic.Dictionary<TKey,TValue>.AlternateLookup<TAlternateKey>`

## Choose collection APIs that expose cheap empty singletons

Return and consume known empty singleton collections for empty results.

- Do: Use Array.Empty<T>(), Enumerable.Empty<T>(), ReadOnlyCollection<T>.Empty, ReadOnlyDictionary<TKey,TValue>.Empty, or collection expressions [] with a suitable target type.
- Instead of: new T[0], new List<T>(), or new ReadOnlyCollection<T>(Array.Empty<T>()) for each empty result.
- Why: Empty arrays and built-in empty read-only collections avoid per-call wrapper and enumerator allocations and compose with LINQ empty fast paths.
- Since .NET 9. Supersedes: .NET 8 added many empty collection enumerator and read-only singleton optimizations; .NET 9 makes Enumerable.Empty<T>() align with Array.Empty<T>().
- Hot path: either | Complexity: low
- APIs: `System.Array.Empty`, `System.Linq.Enumerable.Empty`, `System.Collections.ObjectModel.ReadOnlyCollection<T>.Empty`, `System.Collections.ObjectModel.ReadOnlyDictionary<TKey,TValue>.Empty`

## Prefer collection expressions for fixed literals and spreads

Use C# collection expressions to express arrays, spans, lists, and immutable collections from literals and spreads.

- Do: Use [] and [item1, item2, ..source] where the target type is known.
- Instead of: Manual new List<T>(), repeated Add calls, or broken ImmutableArray<T> collection initializers.
- Why: The compiler can lower them to Array.Empty, inline-array backed spans, presized lists, span AddRange calls, or CollectionBuilder factories.
- Since .NET 8. Supersedes: C# collection initializers that mutate through Add and are especially wrong or inefficient for ImmutableArray<T>.
- Hot path: either | Complexity: low
- APIs: `System.Runtime.CompilerServices.CollectionBuilderAttribute`, `System.Runtime.CompilerServices.InlineArrayAttribute`, `System.Collections.Immutable.ImmutableArray<T>`

## Prefer compile-time string switches for fixed ordinal sets

Use a switch or is pattern for small fixed ordinal string membership tests known at compile time.

- Do: Use a string switch statement, switch expression, or is pattern with constants for ordinal case-sensitive keys.
- Instead of: Build a HashSet<string> or FrozenSet<string> solely for a small static ordinal set.
- Why: The compiler can generate length and character decision trees without building a collection.
- Since .NET 8. Supersedes: Older compiler lowerings that used hash binary search or cascading equality tests for some string patterns.
- Hot path: either | Complexity: low
- APIs: `System.String`, `System.Collections.Frozen.FrozenSet<T>`

## Prefer direct Count or Length over Any for known collections

Read Count, Length, or IsEmpty directly when the static type exposes it.

- Do: Use list.Count != 0, array.Length != 0, string.Length != 0, or collection.IsEmpty.
- Instead of: Enumerable.Any(collection) for a collection that exposes Count, Length, or IsEmpty.
- Why: Direct property access avoids LINQ interface checks, possible enumerator creation, and dispatch overhead.
- Since .NET 8. Supersedes: .NET 5 made Any smarter, but .NET 8 CA1860 recommends direct properties when statically available.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Any`, `System.Collections.Generic.ICollection<T>.Count`, `System.Array.Length`, `System.String.Length`

## Prefer predicate terminal overloads on arrays and lists

Use Any, All, Count, First, and Single predicate overloads directly on arrays and lists in modern .NET.

- Do: Use source.First(predicate), source.Any(predicate), source.All(predicate), source.Count(predicate), or source.Single(predicate).
- Instead of: Keep source.Where(predicate).First() solely because older predicate overloads lacked array/list fast paths.
- Why: .NET 9 added span-backed fast paths that avoid enumerator allocation for T[] and List<T>.
- Since .NET 9. Supersedes: Older guidance that Where(predicate).First() could be faster than First(predicate) on arrays/lists.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Any`, `System.Linq.Enumerable.All`, `System.Linq.Enumerable.Count`, `System.Linq.Enumerable.First`, `System.Linq.Enumerable.Single`

## Prefer single-probe dictionary and set APIs

Use TryAdd, TryGetValue, and direct set Add/Remove return values instead of Contains guards.

- Do: Use Dictionary<TKey,TValue>.TryAdd, IDictionary<TKey,TValue>.TryGetValue, ISet<T>.Add, and ISet<T>.Remove.
- Instead of: ContainsKey before Add or indexer, or Contains before set Add/Remove.
- Why: The mutation or retrieval API already performs the probe, so a guard duplicates hash work.
- Since .NET 8. Supersedes: ContainsKey/Add, ContainsKey/indexer, and Contains-guarded set patterns flagged by CA1864, CA1854, and CA1868.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Generic.Dictionary<TKey,TValue>.TryAdd`, `System.Collections.Generic.IDictionary<TKey,TValue>.TryGetValue`, `System.Collections.Generic.ISet<T>.Add`, `System.Collections.Generic.ISet<T>.Remove`

## Presize mutable collections

Give collections an expected capacity or call EnsureCapacity before bulk insertion.

- Do: Use constructors with capacity, EnsureCapacity, or TryGetNonEnumeratedCount before filling from an IEnumerable<T>.
- Instead of: Start empty and grow one item at a time when the count is known or cheap to get.
- Why: Presizing avoids repeated growth, rehashing, copying, and transient allocations.
- Since .NET 6. Supersedes: Manual guess-and-grow loops before EnsureCapacity was available on List<T>, Stack<T>, and Queue<T>.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Generic.List<T>.EnsureCapacity`, `System.Collections.Generic.Dictionary<TKey,TValue>.EnsureCapacity`, `System.Collections.Generic.HashSet<T>.EnsureCapacity`, `System.Collections.Generic.Queue<T>.EnsureCapacity`, `System.Collections.Generic.Stack<T>.EnsureCapacity`, `System.Linq.Enumerable.TryGetNonEnumeratedCount`

## Rely on numeric LINQ over arrays and lists for common aggregations

Use LINQ Min, Max, Sum, and Average on arrays and lists for supported primitive numeric types when the operation is clear.

- Do: Use Enumerable.Min, Max, Sum, and Average on T[] or List<T> for supported numeric primitives, char, Int128, and UInt128 where applicable.
- Instead of: Manual loops kept only to avoid old LINQ enumerator overhead for these supported cases.
- Why: These operators use span-backed and vectorized paths, often eliminating enumerator allocation and interface dispatch.
- Since .NET 9. Supersedes: .NET 7 introduced int/long array Min/Max vectorization; .NET 8 expanded types and Sum; .NET 9 adds char/Int128/UInt128 and Vector512 support.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Min`, `System.Linq.Enumerable.Max`, `System.Linq.Enumerable.Sum`, `System.Linq.Enumerable.Average`

## Use BitArray built-ins for whole-array bit tests and operations

Use BitArray built-in bulk operations and bit tests instead of per-bit loops.

- Do: Use BitArray.HasAnySet, HasAllSet, And, Or, Xor, and Not.
- Instead of: Loop over every bit with the indexer to check or combine bits.
- Why: They delegate to vectorized storage scans and can use wider vectors on modern hardware.
- Since .NET 9. Supersedes: .NET 8 added HasAnySet/HasAllSet using ContainsAnyExcept; .NET 9 adds Vector512 acceleration for bulk operations.
- Hot path: either | Complexity: low
- APIs: `System.Collections.BitArray.HasAnySet`, `System.Collections.BitArray.HasAllSet`, `System.Collections.BitArray.And`, `System.Collections.BitArray.Or`, `System.Collections.BitArray.Xor`, `System.Collections.BitArray.Not`

## Use FrozenDictionary and FrozenSet for read-mostly data

Build FrozenDictionary or FrozenSet for data created once and queried many times.

- Do: Use ToFrozenDictionary or ToFrozenSet after construction is complete.
- Instead of: ImmutableDictionary or ImmutableSet for large read-mostly tables, or mutable collections exposed as immutable state.
- Why: Frozen collections spend construction time choosing specialized layouts that can beat Dictionary and HashSet for repeated reads.
- Since .NET 9. Supersedes: .NET 8 introduced frozen collections and briefly exposed optimizeForReading overloads; construction improvements removed that need, and .NET 9 improved string strategies.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Frozen.FrozenDictionary<TKey,TValue>`, `System.Collections.Frozen.FrozenSet<T>`, `System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary`, `System.Collections.Frozen.FrozenSet.ToFrozenSet`
- Snippet: [code](../snippets/bcl/collections.md#use-frozendictionary-and-frozenset-for-read-mostly-data)

## Use HashSet when you only need set semantics

Use HashSet<T> instead of Dictionary<T,T> or Dictionary<T,bool> when only membership matters.

- Do: Use HashSet<T>, ToHashSet, or FrozenSet<T> for immutable read-mostly sets.
- Instead of: Dictionary<T,T> or Dictionary<T,bool> used only as a set.
- Why: HashSet stores only keys, so it uses less memory and expresses the operation directly.
- Since .NET 9. Supersedes: .NET 6 LINQ Distinct switched to HashSet<T>; .NET 9 runtime code replaced Dictionary<T,T> set usages with HashSet<T>.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Generic.HashSet<T>`, `System.Linq.Enumerable.ToHashSet`, `System.Collections.Frozen.FrozenSet<T>`

## Use PriorityQueue bulk insertion when priorities match

Insert many items with the same priority using EnqueueRange.

- Do: Use PriorityQueue<TElement,TPriority>.EnqueueRange(IEnumerable<TElement>, TPriority) for same-priority batches.
- Instead of: Call Enqueue once per item when inserting a same-priority batch into an empty queue.
- Why: When the queue is empty, .NET 9 can store the elements directly and skip unnecessary heapify work.
- Since .NET 9. Supersedes: .NET 6 introduced PriorityQueue; .NET 9 removes unnecessary heapify for empty same-priority EnqueueRange.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Generic.PriorityQueue<TElement,TPriority>.EnqueueRange`

## Use SequenceEqual for contiguous sequence comparison

Use Enumerable.SequenceEqual for arrays and lists instead of manual element loops when equality semantics match.

- Do: Use first.SequenceEqual(second) for T[] and List<T> inputs, including comparer overloads when needed.
- Instead of: Manual foreach comparison over IEnumerable<T> that boxes enumerators or misses vectorized span paths.
- Why: It delegates to span-based MemoryExtensions.SequenceEqual for array/list sources and can vectorize comparisons.
- Since .NET 9. Supersedes: .NET 6 accelerated array-array SequenceEqual; .NET 9 extends the span path to List<T> sources.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.SequenceEqual`, `System.MemoryExtensions.SequenceEqual`

## Use Shuffle.Take and Shuffle.Take.Contains directly

Use Enumerable.Shuffle followed by Take or Contains for random sampling workflows.

- Do: Use source.Shuffle().Take(n) and source.Shuffle().Take(n).Contains(value).
- Instead of: source.ToArray(), Random.Shared.Shuffle(array), then Take or Contains over the shuffled array.
- Why: LINQ can use reservoir sampling or probability shortcuts instead of buffering and shuffling the entire input.
- Since .NET 10. Supersedes: Hand-written shuffle iterator patterns before Enumerable.Shuffle existed.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Shuffle`, `System.Linq.Enumerable.Take`, `System.Linq.Enumerable.Contains`, `System.Random.Shuffle`

## Use built-in LINQ terminal optimizations instead of materializing

Let LINQ terminal operators run on the original source when checking membership, first/last elements, counts, or indexed elements.

- Do: Use query.Contains(x), query.First(), query.Last(), query.Count(), or query.ElementAt(i) directly on composed LINQ queries.
- Instead of: Call ToArray or ToList, or manually reimplement OrderBy, Distinct, Reverse, or Union just to call a terminal operator.
- Why: Modern LINQ can bypass expensive intermediate work such as sorting, reversing, distincting, unioning, and buffering.
- Since .NET 10. Supersedes: .NET 9 broadened Iterator<T> specializations; .NET 10 adds many Contains specializations.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Contains`, `System.Linq.Enumerable.First`, `System.Linq.Enumerable.Last`, `System.Linq.Enumerable.Count`, `System.Linq.Enumerable.ElementAt`
- Snippet: [code](../snippets/bcl/collections.md#use-built-in-linq-terminal-optimizations-instead-of-materializing)

## Use new LINQ methods that encode optimized semantics

Use dedicated LINQ methods for sequences, randomization, and outer joins instead of composing older operators by hand.

- Do: Use Enumerable.Sequence, Shuffle, LeftJoin, RightJoin, Order, OrderDescending, Zip with three sources, Take(Range), and ElementAt(Index) where they match the intent.
- Instead of: Manual Sequence loops, custom Shuffle iterators, GroupJoin+SelectMany+DefaultIfEmpty joins, OrderBy(x => x), nested Zip, or Skip+Take for ranges.
- Why: Dedicated methods expose semantics LINQ can optimize with lower allocation and less buffering.
- Since .NET 10. Supersedes: .NET 7 added Order/OrderDescending; .NET 6 added Zip3 and Take(Range); .NET 10 adds Sequence, Shuffle, LeftJoin, and RightJoin.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.Sequence`, `System.Linq.Enumerable.Shuffle`, `System.Linq.Enumerable.LeftJoin`, `System.Linq.Enumerable.RightJoin`, `System.Linq.Enumerable.Order`, `System.Linq.Enumerable.OrderDescending`, `System.Linq.Enumerable.Zip`, `System.Linq.Enumerable.Take`, `System.Linq.Enumerable.ElementAt`

## Use optimized LINQ materializers over manual builders

Use ToArray, ToList, and ToDictionary when materializing standard LINQ pipelines.

- Do: Use Enumerable.ToArray, ToList, ToDictionary, and direct ToDictionary overloads for KeyValuePair or tuple sources.
- Instead of: Hand-written builders that repeatedly Add, or ToDictionary(kvp => kvp.Key, kvp => kvp.Value) for key/value pair sources.
- Why: Modern implementations exploit counts, spans, ArrayPool, CollectionsMarshal.SetCount, and iterator fast paths to reduce allocation and copying.
- Since .NET 10. Supersedes: .NET 8 added delegate-free ToDictionary overloads; .NET 9 improved ToArray/ToList/ToDictionary; .NET 10 speeds Skip/Take ToArray/ToList via span slicing.
- Hot path: either | Complexity: low
- APIs: `System.Linq.Enumerable.ToArray`, `System.Linq.Enumerable.ToList`, `System.Linq.Enumerable.ToDictionary`, `System.Runtime.InteropServices.CollectionsMarshal.SetCount`, `System.Runtime.InteropServices.CollectionsMarshal.AsSpan`

## Use span-based List bulk APIs

Use span-based List extension methods when source data is already contiguous.

- Do: Use List<T>.AddRange(ReadOnlySpan<T>), InsertRange(ReadOnlySpan<T>), and CopyTo(Span<T>) extension methods.
- Instead of: foreach over a span or array and call List<T>.Add for each element.
- Why: They copy with optimized span operations instead of per-element Add calls.
- Since .NET 8. Supersedes: Manual per-element loops used before List<T> had span-based extension methods.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Generic.CollectionExtensions.AddRange`, `System.Collections.Generic.CollectionExtensions.InsertRange`, `System.Collections.Generic.CollectionExtensions.CopyTo`

## Use span-based immutable collection factories

Create immutable collections from ReadOnlySpan<T> when the items are already contiguous or stack allocated.

- Do: Use ImmutableList.Create<T>(ReadOnlySpan<T>) and corresponding immutable collection factories.
- Instead of: CreateBuilder plus repeated Add for small fixed inputs, or params arrays when a span is already available.
- Why: Span factories reduce intermediate allocations compared with builders or params arrays.
- Since .NET 8. Supersedes: .NET 7 added several ImmutableArray and builder range methods; .NET 8 generalized span Create overloads.
- Hot path: either | Complexity: low
- APIs: `System.Collections.Immutable.ImmutableList.Create`, `System.Collections.Immutable.ImmutableStack.Create`, `System.Collections.Immutable.ImmutableQueue.Create`, `System.Collections.Immutable.ImmutableHashSet.Create`, `System.Collections.Immutable.ImmutableSortedSet.Create`, `System.Collections.Immutable.ImmutableArray.Create`

## Prefer ImmutableArray span and marshal APIs when ownership is clear

Use ImmutableArray span APIs and ImmutableCollectionsMarshal to avoid extra arrays and copies in controlled construction paths.

- Do: Use ImmutableArray.Create(ReadOnlySpan<T>) or ImmutableCollectionsMarshal.AsImmutableArray when you own the source array and will not mutate it.
- Instead of: Allocate a builder or copy an already-owned array just to expose ImmutableArray<T>.
- Why: They transfer or consume contiguous data without builder allocations or redundant defensive copying.
- Since .NET 8. Supersedes: .NET 7 added span-based ImmutableArray methods; .NET 8 added ImmutableCollectionsMarshal.AsImmutableArray/AsArray and collection-builder support.
- Hot path: either | Complexity: medium
- APIs: `System.Collections.Immutable.ImmutableArray.Create`, `System.Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray`, `System.Runtime.InteropServices.ImmutableCollectionsMarshal.AsArray`

## Use Native AOT LINQ speed switch when throughput matters

Disable size-optimized LINQ for Native AOT apps when LINQ throughput is more important than code size.

- Do: Set <UseSizeOptimizedLinq>false</UseSizeOptimizedLinq> in the project file after measuring size and throughput.
- Instead of: Assume Native AOT always includes the same speed-focused LINQ specializations as CoreCLR.
- Why: The speed-focused LINQ build keeps more iterator specializations that can avoid allocations and expensive work.
- Since .NET 10. Supersedes: Earlier Native AOT builds made this a build-time library choice rather than an app feature switch.
- Hot path: either | Complexity: medium
- APIs: `System.Linq.Enumerable`
