# Strings and spans performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Cache SearchValues<char> or SearchValues<byte> for set searches

Create SearchValues<T> once for stable delimiter or character sets and pass it to IndexOfAny, LastIndexOfAny, or ContainsAny.

- Do: Store static readonly SearchValues<T> values from SearchValues.Create and reuse them.
- Instead of: Open-code nested membership loops or rebuild delimiter arrays per call.
- Why: The runtime can preselect vectorized ASCII, bitmap, or probabilistic search strategies instead of rebuilding state.
- Since .NET 8. Supersedes: Cached char[] delimiters and manual membership loops from .NET 7 and earlier.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues<T>`, `System.MemoryExtensions.IndexOfAny`, `System.MemoryExtensions.LastIndexOfAny`

## Format UTF-16 directly with MemoryExtensions.TryWrite

Use span-based interpolated string handlers or TryFormat to write formatted text directly into Span<char>.

- Do: Use destination.TryWrite($"{major}.{minor}.{build}.{revision}",out int charsWritten) or value.TryFormat(destination,out charsWritten).
- Instead of: Use string.Format, value.ToString, or interpolation into a temporary string before copying.
- Why: The handler avoids composite format parsing, boxing, object-array allocation, and intermediate strings.
- Since .NET 6. Supersedes: C# 9 lowering to string.Format with object[] and boxing.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.TryWrite`, `System.ISpanFormattable.TryFormat`, `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler`
- Snippet: [code](../snippets/bcl/strings-spans.md#format-utf-16-directly-with-memoryextensionstrywrite)

## Format UTF-8 directly with Utf8.TryWrite

Write interpolated content directly into Span<byte> using Utf8.TryWrite when the output protocol is UTF-8.

- Do: Use Utf8.TryWrite(destination,$"Date: {date:R}",out int bytesWritten).
- Instead of: Build a string and then call Encoding.UTF8.GetBytes.
- Why: It uses interpolated string handlers and IUtf8SpanFormattable to avoid intermediate UTF-16 strings and repeated transcoding.
- Since .NET 8. Supersedes: Utf8Formatter-only manual composition and string-plus-Encoding workflows.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Unicode.Utf8.TryWrite`, `System.IUtf8SpanFormattable.TryFormat`

## Use ASCII-specific char helpers

Use char.IsAsciiDigit, IsAsciiLetter, IsAsciiHexDigit, and related helpers for protocol and ASCII-only parsing.

- Do: Use char.IsAsciiHexDigit(c) or char.IsAsciiLetterOrDigit(c) in parsers.
- Instead of: Use char.IsDigit or char.IsLetter when only ASCII is valid, or hand-roll inconsistent branch-heavy checks.
- Why: They encapsulate efficient ASCII range and casing tricks without paying for full Unicode classification.
- Since .NET 7. Supersedes: Manual ASCII range checks and full-Unicode char classification in ASCII-only hot paths.
- Hot path: yes | Complexity: low
- APIs: `System.Char.IsAsciiDigit`, `System.Char.IsAsciiHexDigit`, `System.Char.IsAsciiLetter`, `System.Char.IsAsciiLetterOrDigit`

## Use ContainsAny when only existence matters

Call ContainsAny or ContainsAnyExcept when the match position is not needed.

- Do: Use input.ContainsAny(searchValues) or input.ContainsAnyExcept(searchValues).
- Instead of: Use input.IndexOfAny(searchValues) >= 0 and discard the index.
- Why: .NET 9 avoids the extra work IndexOfAny needs to compute the exact index.
- Since .NET 9. Supersedes: .NET 8 ContainsAny implemented as IndexOfAny >= 0.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.ContainsAny`, `System.MemoryExtensions.ContainsAnyExcept`

## Use IndexOfAnyExcept for all-but-set validation

Use IndexOfAnyExcept, LastIndexOfAnyExcept, or ContainsAnyExcept to find the first value outside an allowed set.

- Do: Use span.IndexOfAnyExcept(allowed) < 0 or span.ContainsAnyExcept(allowed).
- Instead of: Use a foreach loop that compares every character against every allowed value.
- Why: The APIs express the negative search directly and current implementations avoid O(needle) checks for many sets.
- Since .NET 7. Supersedes: Open-coded all-zero or all-in-set loops; .NET 9 improves SearchValues Except paths.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.IndexOfAnyExcept`, `System.MemoryExtensions.LastIndexOfAnyExcept`, `System.MemoryExtensions.ContainsAnyExcept`

## Use MemoryExtensions.Count for span element counts

Count occurrences of a byte, char, or other element in a span with MemoryExtensions.Count.

- Do: Use span.Count('a') or bytes.Count((byte)'
').
- Instead of: Write a foreach loop that increments a counter for each matching element.
- Why: The library implementation is vectorized and .NET 9 handles tails more efficiently than scalar loops.
- Since .NET 8. Supersedes: Manual count loops; .NET 9 improves the vectorized tail strategy.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.Count`

## Use SearchValues<string> for repeated multi-string search

Precompute SearchValues<string> and call span IndexOfAny or ContainsAny when searching for any of several strings.

- Do: Cache SearchValues.Create(strings,StringComparison.OrdinalIgnoreCase) and use ReadOnlySpan<char>.ContainsAny or IndexOfAny.
- Instead of: Loop over needles with StartsWith at each position or run Contains once per needle.
- Why: .NET 9 can use specialized multi-pattern algorithms and avoid repeated full scans.
- Since .NET 9. Supersedes: Manual switch/prefilter approaches and repeated Contains loops from .NET 8 and earlier.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.SearchValues.Create`, `System.MemoryExtensions.ContainsAny`, `System.MemoryExtensions.IndexOfAny`

## Use char overloads for single-character search

When searching for one character, use char overloads, or keep single-character string needles constant so .NET 9 can specialize them.

- Do: Use text.Contains('$') or text.AsSpan().IndexOf('$').
- Instead of: Use text.Contains(variableString) when the value is always one character.
- Why: Single-character paths avoid substring machinery and can fold to very small code.
- Since .NET 9. Supersedes: General string.Contains(string) for one-character needles in .NET 8 and earlier.
- Hot path: yes | Complexity: low
- APIs: `System.String.Contains(char)`, `System.String.IndexOf(char)`, `System.MemoryExtensions.IndexOf`

## Use span-based parsing and formatting overloads

Choose TryParse, TryFormat, ISpanParsable, ISpanFormattable, and IUtf8SpanFormattable APIs that operate on spans.

- Do: Parse from ReadOnlySpan<char> or ReadOnlySpan<byte> and format into Span<char> or Span<byte>.
- Instead of: Allocate substrings for parsing or call ToString before appending or writing.
- Why: They let parsers and serializers consume or produce slices and buffers without temporary strings.
- Since .NET Core 2.1. Supersedes: Substring plus Parse and ToString plus Append workflows.
- Hot path: yes | Complexity: low
- APIs: `System.ISpanFormattable.TryFormat`, `System.IUtf8SpanFormattable.TryFormat`, `System.ISpanParsable<TSelf>.TryParse`, `System.Buffers.Text.Utf8Parser`, `System.Buffers.Text.Utf8Formatter`

## Create final strings directly with string.Create or span Concat

When a final string is required, create it once with string.Create or concat span slices directly.

- Do: Use string.Create(length,state,static (span,state)=>...) or string.Concat(prefixSpan,middleSpan,suffixSpan).
- Instead of: Use StringBuilder for a few known pieces or Substring each piece before concatenation.
- Why: These APIs avoid intermediate StringBuilder and Substring allocations.
- Since .NET Core 3.0. Supersedes: StringBuilder convenience usage and Substring plus Concat patterns from .NET Core 2.1 and earlier.
- Hot path: either | Complexity: low
- APIs: `System.String.Create`, `System.String.Concat`
- Snippet: [code](../snippets/bcl/strings-spans.md#create-final-strings-directly-with-stringcreate-or-span-concat)

## Decode hex into a destination span

Use Convert.FromHexString span overloads that write into a caller-provided byte span when decoding reusable or hot inputs.

- Do: Use Convert.FromHexString(hex.AsSpan(),destination,out int charsConsumed,out int bytesWritten).
- Instead of: Call Convert.FromHexString(hex) when a reusable destination buffer is available.
- Why: Destination-based decoding removes the byte[] allocation from Convert.FromHexString(string).
- Since .NET 9. Supersedes: Allocating Convert.FromHexString(string) usage from .NET 5 through .NET 8.
- Hot path: either | Complexity: low
- APIs: `System.Convert.FromHexString`

## Slice spans instead of allocating substrings

Carry ReadOnlySpan<char> slices through parsing, trimming, comparison, and concatenation until a string is required.

- Do: Use input.AsSpan(start,length), span.Slice(offset), span.Trim(), span.Equals(...), and span-based Concat.
- Instead of: Call Substring, string.Trim, or ToString after every intermediate step.
- Why: Span slices are views over original data and avoid per-token or per-step string allocations.
- Since .NET Core 2.1. Supersedes: Substring-heavy parsing workflows; .NET 7 also recommends Slice(offset) over Slice(offset,length-offset).
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.AsSpan`, `System.ReadOnlySpan<T>.Slice`, `System.MemoryExtensions.Trim`, `System.String.Concat`
- Snippet: [code](../snippets/bcl/strings-spans.md#slice-spans-instead-of-allocating-substrings)

## Split spans with the allocation-free enumerator

For streaming token iteration, enumerate ranges from MemoryExtensions.Split or SplitAny over ReadOnlySpan<T>.

- Do: Use foreach (Range r in input.AsSpan().Split(',')) and process input.AsSpan(r).Trim().
- Instead of: Use string.Split when you only need to stream through tokens.
- Why: The ref struct enumerator yields Range values, avoids string[] and per-token string allocations, and supports early exit.
- Since .NET 9. Supersedes: string.Split allocation reductions and vectorized internals from older releases for streaming cases.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.Split`, `System.MemoryExtensions.SplitAny`, `System.SpanSplitEnumerator<T>`

## Use Base64Url for URL-safe base64

Use System.Buffers.Text.Base64Url for Base64Url encoding and decoding into byte or char spans.

- Do: Use Base64Url.EncodeToChars, EncodeToUtf8, DecodeFromChars, or DecodeFromUtf8.
- Instead of: Call Convert.TryToBase64Chars and then loop to replace characters and trim padding.
- Why: It shares optimized Base64 machinery and avoids post-processing '+', '/', and '=' yourself.
- Since .NET 9. Supersedes: Custom Base64Url adapters layered on Convert or Base64 in .NET 8 and earlier.
- Hot path: either | Complexity: low
- APIs: `System.Buffers.Text.Base64Url.EncodeToChars`, `System.Buffers.Text.Base64Url.EncodeToUtf8`, `System.Buffers.Text.Base64Url.DecodeFromChars`, `System.Buffers.Text.Base64Url.DecodeFromUtf8`

## Use CommonPrefixLength for prefix-difference detection

Use MemoryExtensions.CommonPrefixLength when you need the length of the shared prefix between two spans.

- Do: Use int n = left.CommonPrefixLength(right) and branch on n.
- Instead of: Manually loop until the first mismatch just to compute a common prefix length.
- Why: It expresses the comparison directly and uses optimized span comparison logic rather than a manual element loop.
- Since .NET 7. Supersedes: Open-coded prefix loops.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.CommonPrefixLength`

## Use Convert hex APIs instead of BitConverter or casing passes

Use Convert.ToHexStringLower, TryToHexString, and TryToHexStringLower for byte-to-hex encoding.

- Do: Use Convert.TryToHexStringLower(bytes,destination,out int charsWritten).
- Instead of: Use BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant() or ToHexString(bytes).ToLowerInvariant().
- Why: The APIs use optimized implementations, and Try variants write into caller buffers with zero allocation.
- Since .NET 9. Supersedes: Convert.ToHexString plus ToLowerInvariant from .NET 5 and BitConverter.Replace patterns.
- Hot path: either | Complexity: low
- APIs: `System.Convert.ToHexString`, `System.Convert.ToHexStringLower`, `System.Convert.TryToHexString`, `System.Convert.TryToHexStringLower`

## Use SequenceEqual and SequenceCompareTo for span comparisons

Compare byte, char, and primitive spans with SequenceEqual or SequenceCompareTo rather than manual loops.

- Do: Use left.AsSpan().SequenceEqual(right) or left.AsSpan().SequenceCompareTo(right).
- Instead of: Compare each element in a for loop unless custom semantics are required.
- Why: These optimized building blocks are vectorized and automatically benefit from runtime improvements.
- Since .NET Core 2.1. Supersedes: Manual memcmp-style loops; .NET Core 3.0 and .NET 7 improved vectorized implementations.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.SequenceEqual`, `System.MemoryExtensions.SequenceCompareTo`

## Use StartsWith and EndsWith with constant ordinal needles

Use StartsWith, EndsWith, SequenceEqual, and Equals with literal needles and ordinal comparisons for fixed tokens.

- Do: Use value.EndsWith(".txt",StringComparison.OrdinalIgnoreCase) or span.StartsWith("https://",StringComparison.Ordinal).
- Instead of: Manually slice, allocate substrings, or case-normalize the whole input first.
- Why: The JIT recognizes constant needles and emits specialized unrolled comparisons, with .NET 9 extending this to EndsWith.
- Since .NET 7. Supersedes: Manual unrolled comparisons and regex generator special cases before JIT recognition.
- Hot path: either | Complexity: low
- APIs: `System.String.StartsWith`, `System.String.EndsWith`, `System.MemoryExtensions.StartsWith`, `System.MemoryExtensions.EndsWith`

## Use StringBuilder span overloads and direct primitive append/insert

Feed StringBuilder with ReadOnlySpan<char> and primitive Append, Insert, or Replace overloads instead of allocating argument strings.

- Do: Use sb.Append(span), sb.Insert(index,value), and sb.Replace(oldValueSpan,newValueSpan).
- Instead of: Call value.ToString(), char.ToString(), or string.Format solely to pass data to StringBuilder.
- Why: Modern StringBuilder implementations use optimized ref-based copying and primitive formatting paths that can avoid ToString allocations.
- Since .NET 7. Supersedes: StringBuilder paths that pinned strings or formatted primitives via ToString; .NET 9 adds span Replace overloads.
- Hot path: either | Complexity: low
- APIs: `System.Text.StringBuilder.Append`, `System.Text.StringBuilder.Insert`, `System.Text.StringBuilder.Replace`

## Use constant ReadOnlySpan<T> data and UTF-8 literals

Represent immutable data as ReadOnlySpan<T> or "..."u8 instead of cached arrays or runtime Encoding.GetBytes.

- Do: Use static ReadOnlySpan<byte> Prefix => "HTTP/1.1"u8 or static ReadOnlySpan<char> Tokens => ['a','b','c'].
- Instead of: Use static readonly byte[] from Encoding.UTF8.GetBytes or mutable cached char[] delimiters.
- Why: The compiler can place constant data in assembly data, avoid array allocation, and expose length to the JIT.
- Since .NET 7. Supersedes: C# 7.3 byte-array-to-ReadOnlySpan optimizations and cached arrays; .NET 9 broadens collection-expression handling.
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`, `System.Text.Encoding.UTF8`, `System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan`
- Snippet: [code](../snippets/bcl/strings-spans.md#use-constant-readonlyspant-data-and-utf-8-literals)

## Use destination Span<Range> split for bounded components

When segment count is known, split into a caller-provided Span<Range> and slice the original input.

- Do: Use MemoryExtensions.Split(source,destinationRanges,separator) and consume source[range].
- Instead of: Call string.Split and parse each returned string for version-like inputs.
- Why: It avoids substrings and string arrays while keeping fixed-shape parsing simple.
- Since .NET 8. Supersedes: Manual IndexOf loops or string.Split for fixed component parsing.
- Hot path: either | Complexity: low
- APIs: `System.MemoryExtensions.Split`, `System.Range`

## Use ordinal span IndexOf and Contains

Use string or span IndexOf and Contains with StringComparison.Ordinal or OrdinalIgnoreCase for non-linguistic text.

- Do: Call span.IndexOf(needle,StringComparison.OrdinalIgnoreCase) or value.Contains(needle,StringComparison.Ordinal).
- Instead of: Use culture defaults, LINQ scans, or ToUpperInvariant/ToLowerInvariant before comparing.
- Why: Ordinal paths are heavily vectorized and avoid culture costs and whole-string casing allocations.
- Since .NET Core 2.1. Supersedes: Manual loops and casing transforms used before optimized ordinal IndexOf paths.
- Hot path: either | Complexity: low
- APIs: `System.String.IndexOf`, `System.String.Contains`, `System.MemoryExtensions.IndexOf`, `System.MemoryExtensions.Contains`

## Use span-based Base64 APIs for caller-owned buffers

When the destination buffer is available, encode and decode Base64 with Convert.TryToBase64Chars or System.Buffers.Text.Base64 span APIs.

- Do: Use Convert.TryToBase64Chars(source,destination,out int charsWritten) or Base64.EncodeToUtf8(source,destination,out _,out _).
- Instead of: Use Convert.ToBase64String and then copy or re-encode the string.
- Why: They avoid intermediate strings or arrays and use vectorized implementations across modern hardware.
- Since .NET Core 2.1. Supersedes: String-returning Base64 workflows when the caller already owns a destination span; .NET 9 improves throughput.
- Hot path: either | Complexity: low
- APIs: `System.Convert.TryToBase64Chars`, `System.Buffers.Text.Base64.EncodeToUtf8`, `System.Buffers.Text.Base64.DecodeFromUtf8`

## Add params ReadOnlySpan<T> overloads to hot params APIs

For APIs that read params T[] synchronously, add a params ReadOnlySpan<T> overload.

- Do: Expose M<T>(params ReadOnlySpan<T> values) beside existing params T[] when safe.
- Instead of: Force every small params call to allocate a T[].
- Why: C# 13 prefers the span overload and can emit inline storage at call sites, avoiding the params array allocation after recompilation.
- Since .NET 9. Supersedes: Only params T[] overloads used before C# 13 params collections.
- Hot path: either | Complexity: medium
- APIs: `System.ReadOnlySpan<T>`, `System.Runtime.CompilerServices.InlineArrayAttribute`
