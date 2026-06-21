# Routing and matching (ASP.NET Core source patterns)

Source-proven performance patterns from this repository, by hot-path component. These are the primary worked examples: when you touch this component, match these patterns. Before adding raw BCL primitives, check [../repo-helpers.md](../repo-helpers.md) for an existing shared helper. Paths are relative to `src`.

## FrozenDictionary alternate span lookup

DictionaryJumpTable stores literal transitions in a FrozenDictionary and uses AlternateLookup<ReadOnlySpan<char>> to probe with a path slice.

- Do: Use ToFrozenDictionary plus GetAlternateLookup<ReadOnlySpan<char>>() for read-only string maps queried by spans.
- Why: Large fan-out literal matching gets O(1) lookups without allocating a substring for the current segment.
- Source: `Http\Routing\src\Matching\DictionaryJumpTable.cs#L14-L42` (path dictionary jump table)
- Hot path: yes | Complexity: low
- APIs: `System.Collections.Frozen.FrozenDictionary`, `FrozenDictionary<TKey,TValue>.AlternateLookup<TAlternateKey>`, `ReadOnlySpan<char>`

## host policy caches host and port locally

HostPolicyJumpTable gets host and port once, then scans precomputed EdgeKey destinations and uses EdgeKey.MatchHost for wildcard and exact host checks.

- Do: Cache request-derived values that may allocate before inner loops and reuse precomputed edge keys.
- Why: HostString host/port access can allocate, so local caching prevents repeated allocation during policy transition selection.
- Source: `Http\Routing\src\Matching\HostMatcherPolicy.cs#L381-L400, Http\Routing\src\Matching\HostMatcherPolicy.cs#L450-L469` (host policy jump table)
- Hot path: yes | Complexity: low
- APIs: `Microsoft.AspNetCore.Http.HostString`, `StringComparison.OrdinalIgnoreCase`

## linear span search for tiny fan-out

LinearSearchJumpTable slices the path segment and loops a small entries array with OrdinalIgnoreCase span comparison.

- Do: Use span comparisons over compact arrays for tiny read-only lookup sets.
- Why: For up to about ten entries, linear search beats dictionary overhead and still avoids allocating segment strings.
- Source: `Http\Routing\src\Matching\LinearSearchJumpTable.cs#L25-L44, Http\Routing\src\Matching\JumpTableBuilder.cs#L74-L86` (path linear jump table)
- Hot path: yes | Complexity: low
- APIs: `ReadOnlySpan<char>`, `MemoryExtensions.Equals`

## readonly structs for hot matcher data

Candidate, PathSegment, DfaState, and policy edge types are readonly structs that carry immutable matcher metadata and offsets.

- Do: Use internal readonly struct for small immutable route matching records.
- Why: Readonly value types avoid heap allocation and make hot loops less prone to defensive copies or mutable shared state.
- Source: `Http\Routing\src\Matching\Candidate.cs#L9-L21, Http\Routing\src\Matching\PathSegment.cs#L6-L15, Http\Routing\src\Matching\DfaState.cs#L8-L26` (matcher data model)
- Hot path: yes | Complexity: low
- APIs: `readonly struct`

## single-entry ASCII span comparison

SingleEntryAsciiJumpTable length-checks the segment, slices the original path, and uses Ascii.EqualsIgnoreCase for the single literal.

- Do: Special-case single ASCII keys with length checks and span-based ordinal ignore-case comparison.
- Why: The common one-edge ASCII node avoids culture-aware string comparison overhead and substring allocation.
- Source: `Http\Routing\src\Matching\SingleEntryAsciiJumpTable.cs#L30-L50` (path single-entry jump table)
- Hot path: yes | Complexity: low
- APIs: `Microsoft.AspNetCore.Routing.Matching.Ascii.EqualsIgnoreCase`, `ReadOnlySpan<char>`

## span tokenizer stores offsets instead of substrings

FastPathTokenizer scans the path with AsSpan and IndexOf('/') and writes PathSegment start/length pairs.

- Do: Represent parsed path pieces as offset-length structs over the original string.
- Why: Route matching can compare and capture against the original string, avoiding substring allocation until a route value is actually needed.
- Source: `Http\Routing\src\Matching\FastPathTokenizer.cs#L15-L42, Http\Routing\src\Matching\PathSegment.cs#L6-L15` (FastPathTokenizer)
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.AsSpan`, `System.MemoryExtensions.IndexOf`, `System.Span<T>`

## stackalloc path segment buffer with SkipLocalsInit

DfaMatcher.MatchAsync stackallocs a Span<PathSegment> sized from the precomputed maximum route depth and tokenizes the request path into it.

- Do: Use [SkipLocalsInit] plus stackalloc Span<T> for bounded per-request scratch state, as in DfaMatcher.MatchAsync.
- Why: The hot path records segment offsets and lengths without allocating arrays, substrings, or zeroing locals before DFA traversal.
- Source: `Http\Routing\src\Matching\DfaMatcher.cs#L31-L47` (DFA matcher request path tokenization)
- Hot path: yes | Complexity: low
- APIs: `System.Span<T>`, `System.Runtime.CompilerServices.SkipLocalsInitAttribute`, `stackalloc`

## CollectionsMarshal.AsSpan over method lists

HttpMethodMatcherPolicy.ContainsHttpMethod gets a List<string> as a span, first checks ReferenceEquals for static HttpMethods instances, then performs semantic comparison.

- Do: Use CollectionsMarshal.AsSpan for tight List<T> loops when the list is not mutated, and split fast identity checks from slower equality checks.
- Why: It avoids enumerator overhead and makes the very common interned/static method match cheap.
- Source: `Http\Routing\src\Matching\HttpMethodMatcherPolicy.cs#L396-L417` (HTTP method matcher policy)
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.InteropServices.CollectionsMarshal.AsSpan`, `object.ReferenceEquals`, `Microsoft.AspNetCore.Http.HttpMethods.Equals`

## HTTP method policy jump table

HttpMethodDestinationsLookup stores common HTTP method destinations in fields and selects by first character and method length before falling back to an extra dictionary.

- Do: Specialize high-frequency known keys into fields and switch expressions, with a dictionary only for uncommon keys.
- Why: Common methods avoid dictionary lookup and reduce comparisons in per-request policy matching.
- Source: `Http\Routing\src\Matching\HttpMethodDestinationsLookup.cs#L8-L21, Http\Routing\src\Matching\HttpMethodDestinationsLookup.cs#L96-L130` (HTTP method matcher policy)
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.AspNetCore.Http.HttpMethods`, `switch expression`, `StringComparison.OrdinalIgnoreCase`

## InlineArray stack storage for small candidate state

DfaMatcher uses a private [InlineArray(4)] struct as a stack-backed CandidateState buffer when no policies and the default selector are used.

- Do: Use [InlineArray] for tiny fixed-size hot-path buffers and fall back to arrays for larger counts.
- Why: Small candidate sets avoid heap allocation while still exposing a Span<CandidateState> to the selector.
- Source: `Http\Routing\src\Matching\DfaMatcher.cs#L99-L111, Http\Routing\src\Matching\DfaMatcher.cs#L335-L345` (DFA matcher candidate state)
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.CompilerServices.InlineArrayAttribute`, `System.Span<T>`

## candidate flags gate expensive work

Candidate precomputes bit flags for defaults, captures, catchalls, complex segments, and constraints and DfaMatcher checks those flags before doing each phase.

- Do: Precompute feature flags on candidate metadata and guard each expensive hot-path step with bit tests.
- Why: Most endpoints skip route value dictionary creation, constraint evaluation, and complex segment processing entirely.
- Source: `Http\Routing\src\Matching\Candidate.cs#L81-L121, Http\Routing\src\Matching\DfaMatcher.cs#L124-L176` (DFA matcher candidate processing)
- Hot path: yes | Complexity: medium
- APIs: `System.FlagsAttribute`

## single-candidate no-policy fast path

DfaMatcher bypasses CandidateSet allocation and endpoint selection when one strict-path candidate, no policies, and the default selector are present.

- Do: Add explicit fast paths for trivial candidate sets before constructing mutable selector state.
- Why: The common exact-match case sets the endpoint directly and returns Task.CompletedTask with no route value dictionary or selector work.
- Source: `Http\Routing\src\Matching\DfaMatcher.cs#L68-L83` (DFA matcher candidate selection)
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.AspNetCore.Http.EndpointHttpContextExtensions.SetEndpoint`, `Task.CompletedTask`

## slot arrays before RouteValueDictionary materialization

DfaMatcher captures route values into a KeyValuePair array prototype and only then creates RouteValueDictionary.FromArray.

- Do: Use preassigned slots and array prototypes for known route values, materializing dictionaries only after captures are complete.
- Why: Array writes are much cheaper than dictionary operations while building candidate route values.
- Source: `Http\Routing\src\Matching\Candidate.cs#L16-L25, Http\Routing\src\Matching\DfaMatcher.cs#L126-L152` (route value capture)
- Hot path: yes | Complexity: medium
- APIs: `Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray`, `System.Array.Copy`

## DFA over precomputed states and jump tables

FindCandidateSet walks an array of DfaState objects by applying path JumpTable transitions, then policy jump tables, before returning prebuilt candidates and policies.

- Do: Precompute matcher state into arrays of readonly structs and drive request-time lookup by integer destinations.
- Why: Per-request matching is reduced to indexed table transitions over spans instead of reparsing route patterns or scanning all endpoints.
- Source: `Http\Routing\src\Matching\DfaMatcher.cs#L209-L232, Http\Routing\src\Matching\DfaState.cs#L8-L26` (DFA matcher transition engine)
- Hot path: yes | Complexity: high
- APIs: `Microsoft.AspNetCore.Routing.Matching.DfaState`, `Microsoft.AspNetCore.Routing.Matching.JumpTable`, `ReadOnlySpan<T>`

## lazy IL-emitted trie with fallback

ILEmitTrieJumpTable initially delegates to a fallback table, starts lazy IL generation, then replaces the delegate with generated ASCII trie code that falls back for non-ASCII segments.

- Do: Lazily initialize generated fast paths and retain a safe fallback for unsupported inputs.
- Why: Startup avoids paying codegen cost, while steady-state ASCII route matching gets a custom branch table.
- Source: `Http\Routing\src\Matching\ILEmitTrieJumpTable.cs#L10-L15, Http\Routing\src\Matching\ILEmitTrieJumpTable.cs#L50-L96` (IL-emitted trie jump table)
- Hot path: yes | Complexity: high
- APIs: `System.Threading.LazyInitializer.EnsureInitialized`, `System.Reflection.Emit.DynamicMethod`, `System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute`

## vectorized ASCII trie comparisons

ILEmitTrieFactory emits code that reads four UTF-16 chars as one ulong and lowercases ASCII characters with non-branching bit operations.

- Do: For very hot ASCII matching, batch characters into machine-word comparisons with explicit non-ASCII bailout.
- Why: Generated jump tables compare longer literals four chars at a time and reduce branches during case-insensitive matching.
- Source: `Http\Routing\src\Matching\ILEmitTrieFactory.cs#L47-L59, Http\Routing\src\Matching\ILEmitTrieFactory.cs#L191-L199, Http\Routing\src\Matching\ILEmitTrieFactory.cs#L211-L237` (IL-emitted trie factory)
- Hot path: yes | Complexity: high
- APIs: `System.Runtime.CompilerServices.Unsafe.ReadUnaligned`, `System.Runtime.InteropServices.MemoryMarshal.GetReference`, `System.Reflection.Emit.ILGenerator`

## builder precomputes candidate slots and captures

DfaMatcherBuilder.CreateCandidate assigns parameter names to slot indexes, captures simple parameters by segment index, and records complex segments and constraints separately.

- Do: Move reflection/pattern analysis into builder code and store compact arrays for the request path.
- Why: Expensive route pattern analysis happens at matcher build time so request matching can use arrays and indexes.
- Source: `Http\Routing\src\Matching\DfaMatcherBuilder.cs#L735-L845` (DFA matcher builder)
- Hot path: either | Complexity: medium
- APIs: `System.Collections.Generic.KeyValuePair<TKey,TValue>[]`, `Array.Empty<T>`

## lazy RouteData collections and small-list restore loops

RouteData delays creating values, data tokens, and routers until accessed, and RouteDataSnapshot.Restore uses manual loops for small router lists instead of Clear/RemoveRange.

- Do: Lazy-initialize optional collections and hand-roll very small list restoration only when profiling proves collection helpers are overhead.
- Why: Legacy routing state avoids allocations for unused collections and avoids native-call overhead in small-list snapshot restore paths.
- Source: `Http\Routing.Abstractions\src\RouteData.cs#L24-L27, Http\Routing.Abstractions\src\RouteData.cs#L68-L110, Http\Routing.Abstractions\src\RouteData.cs#L250-L310` (routing abstractions RouteData)
- Hot path: either | Complexity: medium
- APIs: `System.Collections.Generic.List<T>`, `Microsoft.AspNetCore.Routing.RouteValueDictionary`

## pooled UriBuildingContext and slot arrays for link generation

TemplateBinder receives an ObjectPool<UriBuildingContext>, precomputes route value slots, and builds accepted values from arrays during binding.

- Do: Pool reusable per-operation contexts and preallocate slot templates for known route parameters.
- Why: Outbound routing reuses URI-building state and performs most known-value work in arrays before dictionary creation.
- Source: `Http\Routing\src\Template\DefaultTemplateBinderFactory.cs#L10-L24, Http\Routing\src\Template\TemplateBinder.cs#L21-L35, Http\Routing\src\Template\TemplateBinder.cs#L380-L407` (template binder link generation)
- Hot path: either | Complexity: medium
- APIs: `Microsoft.Extensions.ObjectPool.ObjectPool<T>`, `Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray`

## jump table implementation selected by route fan-out

JumpTableBuilder chooses zero-entry, single-entry ASCII, single-entry, dictionary, or IL-emitted trie implementations based on entry count, ASCII-ness, and platform limits.

- Do: Dispatch to specialized lookup implementations from a central builder using empirically chosen thresholds.
- Why: Each node uses the cheapest lookup strategy for its shape instead of one general-purpose matcher.
- Source: `Http\Routing\src\Matching\JumpTableBuilder.cs#L27-L99` (path jump table builder)
- Hot path: either | Complexity: high
- APIs: `System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled`, `IntPtr.Size`

## SearchValues for route parameter validation

RoutePatternParser defines SearchValues<char> for invalid parameter-name characters and uses span IndexOfAny in the parser and factory validation paths.

- Do: Precompute SearchValues<char> for fixed delimiter sets and call parameterName.AsSpan().IndexOfAny(values).
- Why: Validation checks a small delimiter set using optimized span search rather than repeated character comparisons or regex.
- Source: `Http\Routing\src\Patterns\RoutePatternParser.cs#L15-L24, Http\Routing\src\Patterns\RoutePatternParser.cs#L426-L440, Http\Routing\src\Patterns\RoutePatternFactory.cs#L769-L776` (route pattern parser)
- Hot path: cold | Complexity: low
- APIs: `System.Buffers.SearchValues<T>`, `MemoryExtensions.IndexOfAny`

## host parsing with small stackalloc Range buffer

HostMatcherPolicy.CreateEdgeKey splits host:port into a stackalloc Range[3] buffer and parses spans before creating the normalized EdgeKey.

- Do: Use Span<Range> plus MemoryExtensions.Split for small bounded string splitting.
- Why: Build-time host parsing avoids temporary arrays and string splitting allocations.
- Source: `Http\Routing\src\Matching\HostMatcherPolicy.cs#L155-L188` (host matcher policy builder)
- Hot path: cold | Complexity: low
- APIs: `System.Range`, `MemoryExtensions.Split`, `stackalloc`

## small stackalloc policy scratch buffer

DfaMatcherBuilder uses stackalloc bool[32] to track literals that fail parameter policies, falling back to a heap bool array only for larger literal sets.

- Do: Guard stackalloc with a size threshold and slice to the exact needed length.
- Why: Matcher construction avoids a short-lived heap allocation in the common small-literal case while preserving correctness for large nodes.
- Source: `Http\Routing\src\Matching\DfaMatcherBuilder.cs#L425-L470` (DFA matcher builder literal policy pruning)
- Hot path: cold | Complexity: low
- APIs: `stackalloc`, `System.Span<T>`
