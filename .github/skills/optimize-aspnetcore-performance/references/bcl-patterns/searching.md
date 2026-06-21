# Searching performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Prefer SearchValues for repeated byte and char set search

Cache a SearchValues<byte> or SearchValues<char> for any repeated search over the same byte or character set.

- Do: Store SearchValues.Create(...) in a static readonly field and pass it to ReadOnlySpan<T>.IndexOfAny, ContainsAny, IndexOfAnyExcept, or ContainsAnyExcept.
- Instead of: Calling IndexOfAny with the same ReadOnlySpan<byte> or ReadOnlySpan<char> needle inside a hot loop.
- Why: SearchValues precomputes and caches the best vectorized strategy, avoiding the repeated setup cost of span IndexOfAny with a literal needle.
- Since .NET 8. Supersedes: .NET 7 IndexOfAny(ReadOnlySpan<char>) and IndexOfAny(ReadOnlySpan<byte>) as the primary repeated-set search pattern; keep IndexOfAny as the fallback when SearchValues is unavailable or the search is one-off.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.ContainsAny`, `System.MemoryExtensions.IndexOfAnyExcept`, `System.MemoryExtensions.ContainsAnyExcept`
- Snippet: [code](../snippets/bcl/searching.md#prefer-searchvalues-for-repeated-byte-and-char-set-search)

## Prefer SearchValues<string> for repeated multi-substring search

Use SearchValues<string> when repeatedly searching a string or ReadOnlySpan<char> for any of several substrings.

- Do: Cache SearchValues.Create(string[], StringComparison) and call ReadOnlySpan<char>.IndexOfAny or ContainsAny with the SearchValues<string> instance.
- Instead of: Repeatedly chaining string.IndexOf calls or using a regex alternation only to find one of several literal substrings.
- Why: It enables precomputed vectorized multi-substring search, including case-insensitive Teddy-based paths that avoid repeated scalar IndexOf chains.
- Since .NET 9. Supersedes: Older multi-value substring search with Regex alternation or repeated IndexOf calls in hot paths; SearchValues<string> is the primary guidance on .NET 9+.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.ContainsAny`, `System.StringComparison.Ordinal`, `System.StringComparison.OrdinalIgnoreCase`

## Use ContainsAny when the index is not needed

Use ContainsAny or ContainsAnyExcept when the code only needs to know whether a match exists.

- Do: Replace span.IndexOfAny(values) >= 0 with span.ContainsAny(values) and span.IndexOfAnyExcept(values) >= 0 with span.ContainsAnyExcept(values).
- Instead of: Computing an index and immediately discarding it.
- Why: The Contains variants can avoid the extra work needed to compute and return the exact matching index.
- Since .NET 8. Supersedes: IndexOfAny or IndexOfAnyExcept used only as a boolean test.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.ContainsAny`, `System.MemoryExtensions.ContainsAnyExcept`, `System.Buffers.SearchValues`

## Use Regex.EnumerateMatches for allocation-free match enumeration

Use Regex.EnumerateMatches when iterating matches without needing full Match and capture objects.

- Do: Iterate foreach over regex.EnumerateMatches(input.AsSpan()) for hot match enumeration that only needs index and length.
- Instead of: Regex.Matches or Match.NextMatch when capture-rich Match objects are not needed.
- Why: The ref struct enumerator can store a span input and enumerate ValueMatch results without per-match Match allocations.
- Since .NET 7. Supersedes: Allocation-heavy MatchCollection enumeration for simple match locations.
- Hot path: yes | Complexity: low
- APIs: `System.Text.RegularExpressions.Regex.EnumerateMatches`, `System.Text.RegularExpressions.ValueMatch`, `System.Text.RegularExpressions.ValueMatchEnumerator`

## Use RegexOptions.Compiled only for reused dynamic patterns

Use RegexOptions.Compiled when the pattern is dynamic and reused enough to amortize runtime compilation cost.

- Do: Cache a Regex created with RegexOptions.Compiled for dynamic hot patterns; use GeneratedRegex when the pattern is static.
- Instead of: Compiling cold, one-off, or repeatedly reconstructed regex instances.
- Why: Compiled regexes generate pattern-specific IL and can be much faster during matching, but construction is more expensive than the interpreter.
- Since .NET Core 3.0. Supersedes: Uncached interpreted Regex for reused dynamic hot patterns, but not GeneratedRegex for static patterns.
- Hot path: yes | Complexity: low
- APIs: `System.Text.RegularExpressions.Regex`, `System.Text.RegularExpressions.RegexOptions.Compiled`

## Use SearchValues for validation with any-or-except searches

Validate allowed or disallowed character sets with SearchValues and the AnyExcept family.

- Do: Use input.ContainsAnyExcept(allowedSearchValues) or input.IndexOfAny(disallowedSearchValues) for validators such as base64, JSON, URL, and encoder scans.
- Instead of: Manual character-by-character loops or repeatedly rebuilding large allowed/disallowed character lists.
- Why: The implementation can vectorize large ASCII, mixed ASCII/non-ASCII, and probabilistic-map searches while keeping validation allocation-free.
- Since .NET 8. Supersedes: Custom Vector<T> scanners and repeated IndexOfAny literal needles used before SearchValues.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.ContainsAnyExcept`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.IndexOfAnyExcept`

## Expose literals, anchors, and fixed offsets in regex patterns

Structure regex patterns so the optimizer can find literal prefixes, anchors, fixed-offset literals, and useful starting character sets.

- Do: Keep shared anchors and prefixes factored, prefer clear literal substrings, and avoid obscuring required literals behind unnecessary alternations or zero-width wrappers.
- Instead of: Patterns that hide simple literals or anchors in forms that force the engine to test every input position.
- Why: The engines use those shapes to skip impossible starting positions with IndexOf, IndexOfAny, SearchValues, and anchor checks before running the full match.
- Since .NET 7. Supersedes: Relying on bumpalong matching at every input position; .NET 10 recognizes more lookahead, alternation, and anchor shapes automatically.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.RegularExpressions.Regex`, `System.MemoryExtensions.IndexOf`, `System.MemoryExtensions.IndexOfAny`, `System.Buffers.SearchValues`

## Use atomic groups when backtracking cannot help

Add atomic groups around loops when you can prove that giving characters back cannot change the successful match.

- Do: Use regex atomic groups such as (?>...) for loops followed by disjoint constructs when preserving semantics is clear.
- Instead of: Ambiguous greedy or lazy loops that repeatedly backtrack even though the following pattern is disjoint.
- Why: Atomic groups prevent needless retry work and can turn potentially large backtracking searches into a single forward pass.
- Since .NET 7. Supersedes: Manual one-character-at-a-time backtracking patterns; .NET 10 auto-atomicity handles many more cases but explicit atomic groups remain useful for clarity and older targets.
- Hot path: yes | Complexity: high
- APIs: `System.Text.RegularExpressions.Regex`, `System.Text.RegularExpressions.RegexOptions.Compiled`, `System.Text.RegularExpressions.GeneratedRegexAttribute`

## Avoid unnecessary regex captures

Use non-capturing groups or RegexOptions.ExplicitCapture when captures are only for grouping.

- Do: Write (?:...) for grouping-only constructs or specify RegexOptions.ExplicitCapture when unnamed captures are not consumed.
- Instead of: Using (...) by default for grouping when Group and Capture data are never read.
- Why: Avoiding capture bookkeeping reduces work during matching and can let the optimizer remove captures in negative lookarounds.
- Since .NET 7. Supersedes: Capture-by-default regex authoring in hot paths; .NET 10 removes more useless captures inside negative lookarounds.
- Hot path: either | Complexity: low
- APIs: `System.Text.RegularExpressions.RegexOptions.ExplicitCapture`, `System.Text.RegularExpressions.Group`, `System.Text.RegularExpressions.Capture`

## Do not specify unused RegexOptions

Avoid RegexOptions values that do not affect the pattern, such as CultureInvariant without IgnoreCase or inline case-insensitivity.

- Do: Pass only behaviorally necessary RegexOptions and remove CultureInvariant when there is no case-insensitive matching.
- Instead of: Cargo-culting RegexOptions.CultureInvariant, Compiled, or other options onto every regex.
- Why: Unused options can add overhead, inhibit trimming, and force overloads that keep more regex infrastructure reachable.
- Since .NET 7. Supersedes: Option-heavy regex construction that prevents cheaper constructors and trimming.
- Hot path: either | Complexity: low
- APIs: `System.Text.RegularExpressions.RegexOptions`, `System.Text.RegularExpressions.RegexOptions.CultureInvariant`, `System.Text.RegularExpressions.RegexOptions.IgnoreCase`

## Prefer GeneratedRegex for stable hot regexes

Use the regex source generator for regex patterns known at compile time.

- Do: Declare a partial method or property annotated with [GeneratedRegex(pattern, options)] and reuse the generated Regex instance.
- Instead of: Constructing new Regex(pattern, RegexOptions.Compiled) repeatedly for a constant pattern.
- Why: GeneratedRegex moves code generation to build time, emits optimized C#, enables static SearchValues fields, avoids runtime reflection emit, and improves trimming and startup.
- Since .NET 7. Supersedes: RegexOptions.Compiled as the primary choice for compile-time-known patterns.
- Hot path: either | Complexity: low
- APIs: `System.Text.RegularExpressions.GeneratedRegexAttribute`, `System.Text.RegularExpressions.Regex`
- Snippet: [code](../snippets/bcl/searching.md#prefer-generatedregex-for-stable-hot-regexes)

## Replace simple regexes with direct search APIs

Use IndexOf, Contains, SearchValues, or char classification helpers when the problem is a simple literal, set, or ASCII-class search.

- Do: Use string.Contains, span.IndexOf, span.IndexOfAny(SearchValues), or char.IsAsciiDigit and related helpers for simple predicates.
- Instead of: A regex for a literal substring, a small character set, or a simple ASCII classification check.
- Why: Direct search APIs avoid regex engine setup, Match allocation risks, and pattern analysis overhead while still using optimized vectorized primitives.
- Since .NET 7. Supersedes: Regex use that was just as doable with cheaper IndexOf or char helper APIs.
- Hot path: either | Complexity: low
- APIs: `System.String.Contains`, `System.MemoryExtensions.IndexOf`, `System.MemoryExtensions.IndexOfAny`, `System.Buffers.SearchValues`, `System.Char.IsAsciiDigit`, `System.Char.IsAsciiLetterOrDigit`

## Search spans directly instead of slicing strings

Use ReadOnlySpan<char> and ReadOnlySpan<byte> search APIs to process subranges without allocating substrings or arrays.

- Do: Convert once with AsSpan, slice spans, and pass spans to IndexOf, IndexOfAny, Regex.IsMatch, Regex.Count, or Regex.EnumerateMatches overloads.
- Instead of: Substring, ToArray, or ToString just to call a search API over a subrange.
- Why: Span inputs expose canonical bounds to the JIT, reduce bounds checks, and avoid copying the searched region.
- Since .NET 7. Supersedes: String-only Regex and substring-based search flows that predate span overloads.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.AsSpan`, `System.ReadOnlySpan<T>.Slice`, `System.Text.RegularExpressions.Regex.IsMatch`, `System.Text.RegularExpressions.Regex.Count`, `System.Text.RegularExpressions.Regex.EnumerateMatches`

## Use CountAny and ReplaceAny for span-wide scans

Use CountAny and ReplaceAny for common count or replace operations over a target set.

- Do: Call MemoryExtensions.CountAny or ReplaceAny with the relevant values or SearchValues where available.
- Instead of: Manual loops that call IndexOfAny repeatedly when the operation is simply count or replace.
- Why: Purpose-built MemoryExtensions helpers let the runtime apply optimized vectorized implementations instead of hand-written scan loops.
- Since .NET 10. Supersedes: Repeated IndexOfAny loops for simple counting and replacement transforms.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.CountAny`, `System.MemoryExtensions.ReplaceAny`, `System.Buffers.SearchValues`

## Use IsMatch instead of Match().Success

Call Regex.IsMatch when the code only needs a yes or no answer.

- Do: Replace Regex.Match(input).Success and regex.Match(input).Success with Regex.IsMatch or regex.IsMatch.
- Instead of: Regex.Match(...).Success for existence checks.
- Why: IsMatch avoids allocating Match data and can skip work needed to compute exact bounds and captures.
- Since .NET 10. Supersedes: Older Match().Success idiom; analyzer CA1874 flags this replacement.
- Hot path: either | Complexity: low
- APIs: `System.Text.RegularExpressions.Regex.IsMatch`, `System.Text.RegularExpressions.Regex.Match`

## Use Regex.Count instead of Matches().Count

Use Regex.Count when only the number of matches is needed.

- Do: Replace regex.Matches(input).Count with regex.Count(input), including span overloads when appropriate.
- Instead of: Materializing MatchCollection just to count matches.
- Why: Count can avoid allocating a MatchCollection and can avoid capture collection work that the result does not need.
- Since .NET 7. Supersedes: Matches(...).Count idiom; .NET 10 analyzer CA1875 flags this replacement.
- Hot path: either | Complexity: low
- APIs: `System.Text.RegularExpressions.Regex.Count`, `System.Text.RegularExpressions.Regex.Matches`

## Use generic MemoryExtensions overloads with comparers

In generic code, use the .NET 10 MemoryExtensions overloads that accept optional IEqualityComparer<T> or IComparer<T>.

- Do: Delegate array or List<T> backed generic searches to span.Contains, IndexOf, or related MemoryExtensions overloads with the default or supplied comparer.
- Instead of: Always falling back to IEnumerable<T> enumeration or hand-written comparer loops.
- Why: They allow unconstrained generic code and comparer-based searches to use optimized span paths, including vectorized defaults for suitable types.
- Since .NET 10. Supersedes: Older MemoryExtensions overloads that required IEquatable<T> or IComparable<T> constraints.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.Contains`, `System.MemoryExtensions.IndexOf`, `System.MemoryExtensions.SequenceCompareTo`, `System.Collections.Generic.IEqualityComparer<T>`, `System.Collections.Generic.IComparer<T>`

## Use plain IndexOf and IndexOfAny as the fallback search primitive

Use span-based IndexOf, IndexOfAny, LastIndexOf, and LastIndexOfAny for one-off searches or when targeting frameworks without SearchValues.

- Do: Call ReadOnlySpan<T>.IndexOf, IndexOfAny, LastIndexOf, or LastIndexOfAny directly for one-off searches, small simple needles, or older target frameworks.
- Instead of: Allocating substrings, using LINQ scans, or reaching for Regex for simple literal or small-set searches.
- Why: The span overloads are broadly optimized and avoid allocations, but they do not cache per-needle analysis across calls.
- Since .NET Core 2.1. Supersedes: For repeated set searches on .NET 8+, SearchValues<byte> and SearchValues<char> supersede direct IndexOfAny with a repeated needle.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.IndexOf`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.LastIndexOf`, `System.MemoryExtensions.LastIndexOfAny`, `System.String.IndexOf`

## Use range search APIs for contiguous character ranges

Use IndexOfAnyInRange, IndexOfAnyExceptInRange, and related range APIs for searches such as digits, surrogates, and ASCII ranges.

- Do: Call span.IndexOfAnyInRange('0', '9'), span.IndexOfAnyExceptInRange(...), or the LastIndexOf variants when the target is a contiguous range.
- Instead of: A for loop with char comparisons at every position or a regex just to find a range.
- Why: Range-aware APIs vectorize contiguous range checks and replace scalar loops over each character.
- Since .NET 8. Supersedes: Manual per-character range scans and older Regex-generated scalar loops.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.IndexOfAnyInRange`, `System.MemoryExtensions.IndexOfAnyExceptInRange`, `System.MemoryExtensions.LastIndexOfAnyInRange`, `System.MemoryExtensions.LastIndexOfAnyExceptInRange`

## Choose NonBacktracking for predictable regex time

Use RegexOptions.NonBacktracking for supported patterns where predictable linear-time behavior matters more than best-case speed.

- Do: Use RegexOptions.NonBacktracking for untrusted inputs or patterns susceptible to exponential backtracking, and still set timeouts for externally supplied patterns.
- Instead of: Relying on a backtracking engine for untrusted patterns with nested quantifiers or ambiguous alternations.
- Why: The non-backtracking engine avoids catastrophic backtracking and provides worst-case processing time linear in input length.
- Since .NET 7. Supersedes: Backtracking regex as the default for risk-sensitive searches when the pattern uses only NonBacktracking-supported constructs.
- Hot path: either | Complexity: medium
- APIs: `System.Text.RegularExpressions.RegexOptions.NonBacktracking`, `System.Text.RegularExpressions.Regex`
