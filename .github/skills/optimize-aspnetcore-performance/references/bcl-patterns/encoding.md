# Encoding performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Compare ASCII bytes and chars directly

Compare UTF-8 byte spans with UTF-16 char spans directly when the contents are ASCII.

- Do: Use Ascii.EqualsIgnoreCase(ReadOnlySpan<char>, ReadOnlySpan<byte>) or the matching overload for your span types.
- Instead of: Decode bytes to string or write a per-character upper/lower loop before comparing.
- Why: Ascii.Equals and Ascii.EqualsIgnoreCase avoid transcoding and are much faster than open-coded ordinal ASCII comparisons.
- Since .NET 8. Supersedes: Manual EqualsOrdinalAsciiIgnoreCase helpers used before .NET 8.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Ascii.Equals`, `System.Text.Ascii.EqualsIgnoreCase`

## Encode UTF-8 into caller-owned spans

When converting variable UTF-16 text to UTF-8, size the destination and encode directly into the final span.

- Do: Use Encoding.UTF8.GetByteCount plus Encoding.UTF8.GetBytes(ReadOnlySpan<char>, Span<byte>) or Encoding.UTF8.TryGetBytes.
- Instead of: Encoding.UTF8.GetBytes(string) followed by copying the returned byte array.
- Why: Encoding directly avoids temporary byte arrays and lets the caller use stack, pooled, native, or I/O buffers.
- Since .NET Core 2.1. Supersedes: Temporary byte[] allocation before writing encoded data to another buffer.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Encoding.UTF8`, `System.Text.Encoding.GetByteCount`, `System.Text.Encoding.GetBytes`, `System.Text.Encoding.TryGetBytes`

## Format interpolated strings into spans

Write formatted text directly into Span<char> or Span<byte> instead of allocating a formatted string.

- Do: Use MemoryExtensions.TryWrite for Span<char> and Utf8.TryWrite for Span<byte>.
- Instead of: string.Format or interpolated string allocation followed by copying to a buffer.
- Why: Interpolated string handlers use TryFormat paths and avoid boxing and intermediate strings.
- Since .NET 6. Supersedes: String.Format and ToString based buffer filling in hot paths.
- Hot path: yes | Complexity: low
- APIs: `System.MemoryExtensions.TryWrite`, `System.Text.Unicode.Utf8.TryWrite`, `System.ISpanFormattable.TryFormat`, `System.IUtf8SpanFormattable.TryFormat`

## Format values directly as UTF-8

Use UTF-8 span formatting when writing formatted values to byte-oriented protocols.

- Do: Use Utf8.TryWrite or call IUtf8SpanFormattable.TryFormat through supported types such as numeric primitives, DateTime, Guid, Version, IPAddress, and IPNetwork.
- Instead of: value.ToString(), string interpolation, then Encoding.UTF8.GetBytes.
- Why: IUtf8SpanFormattable lets supported values format directly into Span<byte> without creating a string or transcoding UTF-16.
- Since .NET 8. Supersedes: UTF-16 formatting followed by UTF-8 encoding for byte protocols.
- Hot path: yes | Complexity: low
- APIs: `System.IUtf8SpanFormattable`, `System.Text.Unicode.Utf8.TryWrite`

## Hex decode into a span

Decode hex text into a caller-provided byte span when the destination can be reused or stack allocated.

- Do: Use Convert.FromHexString(ReadOnlySpan<char>, Span<byte>, out int charsConsumed, out int bytesWritten).
- Instead of: Convert.FromHexString(string) when the result is only a temporary buffer.
- Why: The span overload avoids allocating a new byte array for every decode and uses the optimized decoder.
- Since .NET 9. Supersedes: Allocating Convert.FromHexString(string) in hot paths.
- Hot path: yes | Complexity: low
- APIs: `System.Convert.FromHexString`

## Hex encode into a span

Format hex into a caller-provided char span when the destination buffer is already available.

- Do: Use Convert.TryToHexString or Convert.TryToHexStringLower with a destination Span<char> sized at 2 * input.Length.
- Instead of: Convert.ToHexString when you will immediately copy the string into another buffer.
- Why: TryToHexString and TryToHexStringLower remove the output string allocation and are much faster in tight loops.
- Since .NET 9. Supersedes: Allocating Convert.ToHexString for intermediate hex text.
- Hot path: yes | Complexity: low
- APIs: `System.Convert.TryToHexString`, `System.Convert.TryToHexStringLower`

## Parse UTF-8 primitives directly where available

Use UTF-8 parsing overloads for types that implement IUtf8SpanParsable when your input is already UTF-8.

- Do: Use Guid.Parse(ReadOnlySpan<byte>), Guid.TryParse(ReadOnlySpan<byte>, out Guid), Version.Parse(ReadOnlySpan<byte>), or generic IUtf8SpanParsable<TSelf> code.
- Instead of: Encoding.UTF8.GetString(bytes) followed by Guid.Parse or Version.Parse.
- Why: Direct parsing avoids transcoding to UTF-16 and can be faster than generic Utf8Parser paths for supported types.
- Since .NET 10. Supersedes: Transcode-then-parse or Utf8Parser.TryParse for whole Guid inputs before .NET 10.
- Hot path: yes | Complexity: low
- APIs: `System.IUtf8SpanParsable<TSelf>`, `System.Guid.Parse`, `System.Guid.TryParse`, `System.Version.Parse`

## Transcode ASCII with Ascii.FromUtf16 and ToUtf16

When UTF-16 text is guaranteed ASCII, transcode into a caller-provided byte span with Ascii.FromUtf16, or widen ASCII bytes with Ascii.ToUtf16.

- Do: Use Ascii.FromUtf16(sourceChars, destinationBytes, out bytesWritten) and Ascii.ToUtf16(sourceBytes, destinationChars, out charsWritten).
- Instead of: Encoding.UTF8.GetBytes(string) or Encoding.UTF8.GetString(byte[]) when ASCII is already known and a destination buffer exists.
- Why: ASCII transcoding is a narrow or widen operation and the API avoids allocating strings or byte arrays.
- Since .NET 8. Supersedes: Custom narrow UTF-16 to ASCII loops used before .NET 8.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Ascii.FromUtf16`, `System.Text.Ascii.ToUtf16`

## Use Base64Url for URL-safe Base64

Use the .NET 9 Base64Url type for URL-safe Base64 encoding and decoding.

- Do: Call Base64Url.EncodeToChars, Base64Url.EncodeToUtf8, Base64Url.DecodeFromChars, or Base64Url.DecodeFromUtf8.
- Instead of: Convert.TryToBase64Chars followed by replacing '+' with '-', '/' with '_', and trimming '='.
- Why: It writes the URL-safe alphabet directly, omits padding as required, and avoids a second pass replacing '+', '/', and '='.
- Since .NET 9. Supersedes: ASP.NET-style custom Base64Url wrappers layered on Convert or Base64 before .NET 9.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.Text.Base64Url`, `System.Buffers.Text.Base64Url.EncodeToChars`, `System.Buffers.Text.Base64Url.EncodeToUtf8`, `System.Buffers.Text.Base64Url.DecodeFromUtf8`

## Use System.Buffers.Text.Base64 for UTF-8 Base64

Encode and decode Base64 directly between byte spans when the textual representation is UTF-8 or ASCII bytes.

- Do: Use Base64.EncodeToUtf8, Base64.DecodeFromUtf8, and Base64.GetMaxEncodedToUtf8Length with caller-owned buffers.
- Instead of: Convert to a string with Convert.ToBase64String and then UTF-8 encode it, or decode UTF-8 to string before Base64 decoding.
- Why: Base64.EncodeToUtf8 and DecodeFromUtf8 avoid UTF-16 strings and benefit from optimized vectorized implementations.
- Since .NET Core 2.1. Supersedes: String-based Base64 pipelines when the surrounding protocol is UTF-8.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.Text.Base64.EncodeToUtf8`, `System.Buffers.Text.Base64.DecodeFromUtf8`, `System.Buffers.Text.Base64.GetMaxEncodedToUtf8Length`

## Use System.Text.Ascii for ASCII-only text

Use System.Text.Ascii when the data is known to be ASCII and you need compare, validate, case-map, trim, or transcode spans.

- Do: Call Ascii.Equals, Ascii.EqualsIgnoreCase, Ascii.IsValid, Ascii.ToUpper, Ascii.ToLower, Ascii.Trim, Ascii.FromUtf16, or Ascii.ToUtf16 on spans.
- Instead of: Open-coded ASCII loops, string conversions, or full Encoding paths for ASCII-only data.
- Why: The dedicated routines are vectorized and avoid slower hand-written loops or full Unicode work.
- Since .NET 8. Supersedes: Custom ASCII helpers used in libraries before .NET 8.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Ascii`
- Snippet: [code](../snippets/bcl/encoding.md#use-systemtextascii-for-ascii-only-text)

## Validate Base64 with Base64.IsValid

Use Base64.IsValid to check UTF-8 or UTF-16 Base64 payloads and optionally get decoded size before decoding.

- Do: Call Base64.IsValid(input, out int decodedLength) before allocating or decoding.
- Instead of: Hand-parse the alphabet, padding, and whitespace with scalar loops.
- Why: The validation is vectorized, handles whitespace consistently with Convert, and replaces slow manual counting loops.
- Since .NET 8. Supersedes: Manual TryCountBase64-style validators before .NET 8.
- Hot path: yes | Complexity: low
- APIs: `System.Buffers.Text.Base64.IsValid`

## Write Base64 chars into a destination span

When you need UTF-16 Base64 text, encode into a caller-provided char span instead of creating intermediate strings or arrays.

- Do: Use Convert.TryToBase64Chars(ReadOnlySpan<byte>, Span<char>, out int charsWritten).
- Instead of: Convert.ToBase64String followed by copying into another buffer.
- Why: Convert.TryToBase64Chars is optimized and avoids destination allocations in reusable-buffer pipelines.
- Since .NET Core 2.1. Supersedes: Array-returning or string-returning Base64 APIs in span-based code.
- Hot path: yes | Complexity: low
- APIs: `System.Convert.TryToBase64Chars`, `System.Convert.ToBase64String`

## Decode UTF-8 into spans only when transcoding is required

If an API still requires UTF-16, decode UTF-8 into a stack or pooled char span before falling back to string allocation.

- Do: Use Encoding.UTF8.TryGetChars(sourceBytes, destinationChars, out charsWritten) and only call Encoding.UTF8.GetString on fallback paths.
- Instead of: Always call Encoding.UTF8.GetString(byte[]) before parsing or inspection.
- Why: TryGetChars can avoid a transient string for small inputs while preserving correctness for larger inputs with fallback logic.
- Since .NET 8. Supersedes: Unconditional UTF-8 to string transcoding in parsers.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Encoding.TryGetChars`, `System.Text.Encoding.UTF8.GetString`

## Cache CompositeFormat for runtime format strings

Parse runtime-provided composite format strings once and reuse the CompositeFormat instance.

- Do: Use CompositeFormat.Parse once, then pass the CompositeFormat to string.Format, StringBuilder.AppendFormat, or MemoryExtensions.TryWrite overloads.
- Instead of: Call string.Format with the same runtime format string on every request.
- Why: Reusing CompositeFormat avoids repeated parsing and generic overloads reduce boxing of value-type arguments.
- Since .NET 8. Supersedes: Repeated string.Format parsing for resource or configuration format strings.
- Hot path: either | Complexity: low
- APIs: `System.Text.CompositeFormat`, `System.Text.CompositeFormat.Parse`, `System.String.Format`, `System.Text.StringBuilder.AppendFormat`, `System.MemoryExtensions.TryWrite`

## Prefer UTF-8 literals for constant protocol bytes

Represent constant UTF-8 text as u8 literals rather than encoding string literals at run time.

- Do: Use "literal"u8 for ReadOnlySpan<byte> constants such as headers, delimiters, and protocol tokens.
- Instead of: Encoding.UTF8.GetBytes("literal") for constants.
- Why: The compiler stores the bytes in the assembly data section, removing the Encoding.UTF8.GetBytes allocation and encoding cost.
- Since .NET 7. Supersedes: Runtime UTF-8 encoding of constant strings before C# 11.
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<byte>`, `System.Text.Encoding.UTF8.GetBytes`

## Use Convert span overloads for Base64 input slices

Pass ReadOnlySpan<byte> slices to Convert Base64 APIs instead of copying input ranges to new arrays.

- Do: Use Convert.ToBase64String(ReadOnlySpan<byte>) or Convert.TryToBase64Chars(ReadOnlySpan<byte>, Span<char>, out int).
- Instead of: input.Skip(offset).Take(count).ToArray() before Convert.ToBase64String.
- Why: Span overloads let Convert encode the exact range without a temporary byte array.
- Since .NET Core 2.1. Supersedes: Offset/count or copied-array Base64 call sites when spans are available.
- Hot path: either | Complexity: low
- APIs: `System.Convert.ToBase64String`, `System.Convert.TryToBase64Chars`
- Snippet: [code](../snippets/bcl/encoding.md#use-convert-span-overloads-for-base64-input-slices)

## Use Convert.ToHexString and FromHexString

Use Convert hex APIs for byte-to-hex and hex-to-byte conversions.

- Do: Call Convert.ToHexString for uppercase hex and Convert.FromHexString for decoding.
- Instead of: BitConverter.ToString(bytes).Replace("-", "") or hand-written nibble conversion loops.
- Why: The built-in implementations are heavily optimized and avoid delimiter removal or custom scalar loops.
- Since .NET 5. Supersedes: BitConverter.ToString plus Replace patterns used before .NET 5.
- Hot path: either | Complexity: low
- APIs: `System.Convert.ToHexString`, `System.Convert.FromHexString`
- Snippet: [code](../snippets/bcl/encoding.md#use-converttohexstring-and-fromhexstring)

## Use lowercase hex APIs directly

Generate lowercase hex directly with Convert.ToHexStringLower or Convert.TryToHexStringLower.

- Do: Use Convert.ToHexStringLower for strings or Convert.TryToHexStringLower for a destination char span.
- Instead of: Convert.ToHexString(bytes).ToLowerInvariant().
- Why: Direct lowercase formatting avoids allocating uppercase text and then allocating or scanning again for ToLowerInvariant.
- Since .NET 9. Supersedes: ToHexString plus ToLowerInvariant before .NET 9.
- Hot path: either | Complexity: low
- APIs: `System.Convert.ToHexStringLower`, `System.Convert.TryToHexStringLower`
