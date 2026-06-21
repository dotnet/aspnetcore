# Numerics and primitives performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Use BigInteger built-in parsing and formatting

Use BigInteger.Parse, TryParse, and TryFormat for decimal, hex, and binary large integers.

- Do: Call BigInteger.Parse/TryParse with NumberStyles.HexNumber or BinaryNumber and BigInteger.TryFormat into a caller buffer.
- Instead of: Implement digit-by-digit large integer conversion in library code.
- Why: The built-in code switches algorithms by input size, vectorizes large hex/binary parsing, and avoids formatting temporaries in current releases.
- Since .NET 9. Supersedes: .NET 7 introduced lower-complexity decimal parsing for huge values; .NET 9 lowers the threshold and improves hex, binary, and TryFormat allocations.
- Hot path: yes | Complexity: low
- APIs: `System.Numerics.BigInteger.Parse`, `System.Numerics.BigInteger.TryParse`, `System.Numerics.BigInteger.TryFormat`, `System.Globalization.NumberStyles`

## Use BitOperations for bit-twiddling primitives

Use BitOperations helpers for population counts, leading/trailing zero counts, rotations, powers of two, and CRC32C.

- Do: Call BitOperations.PopCount, LeadingZeroCount, TrailingZeroCount, RotateLeft, RotateRight, IsPow2, RoundUpToPowerOf2, or Crc32C.
- Instead of: Open-code bit loops, lookup tables, or manual CRC32C byte walkers.
- Why: The helpers map to hardware intrinsics where available and avoid slow table or branch-heavy manual implementations.
- Since .NET 8. Supersedes: Manual CRC32C loops and older ad hoc bit tricks are superseded by dedicated System.Numerics helpers.
- Hot path: yes | Complexity: low
- APIs: `System.Numerics.BitOperations.PopCount`, `System.Numerics.BitOperations.LeadingZeroCount`, `System.Numerics.BitOperations.TrailingZeroCount`, `System.Numerics.BitOperations.IsPow2`, `System.Numerics.BitOperations.Crc32C`

## Use TensorPrimitives for bulk numeric span operations

For large spans of numeric data, call TensorPrimitives instead of scalar loops for supported operations.

- Do: Call TensorPrimitives.Add, Divide, Cosh, HammingDistance, SoftMax, ConvertTruncating, LeadingZeroCount, Clamp, Average, or related APIs on spans.
- Instead of: Write a per-element loop around Math, MathF, numeric operators, or bit helpers when a TensorPrimitives overload exists.
- Why: TensorPrimitives uses shared vectorized implementations over Vector128, Vector256, and Vector512 and covers hundreds of scalar-like operations on spans.
- Since .NET 10. Supersedes: .NET 8 introduced float-only TensorPrimitives; .NET 9 added generic overloads; .NET 10 adds more operations and broader vectorization including Half and nint/nuint cases.
- Hot path: yes | Complexity: low
- APIs: `System.Numerics.Tensors.TensorPrimitives`, `System.Numerics.Tensors.TensorPrimitives.Add`, `System.Numerics.Tensors.TensorPrimitives.Divide`, `System.Numerics.Tensors.TensorPrimitives.HammingDistance`, `System.Numerics.Tensors.TensorPrimitives.SoftMax`

## Use tensor compound operators for in-place updates

Use Tensor<T> compound operators when mutating large tensors in place.

- Do: Use t1 += t2 and related compound operators on System.Numerics.Tensors tensor types when mutation is intended.
- Instead of: Write t1 = t1 + t2 repeatedly when the target tensor can be updated in place.
- Why: User-defined compound operators can avoid allocating a new tensor for each arithmetic step.
- Since .NET 10. Supersedes: Earlier C# compound operators expanded to allocate-producing binary operators for tensor-like types.
- Hot path: yes | Complexity: low
- APIs: `System.Numerics.Tensors.Tensor<T>`, `System.Numerics.Tensors.TensorSpan<T>`, `System.Numerics.Tensors.ReadOnlyTensorSpan<T>`, `System.Numerics.Tensors.ITensor<TSelf,T>`

## Use BigInteger byte-span APIs

Construct and write BigInteger values using span-based byte APIs.

- Do: Use BigInteger(ReadOnlySpan<byte>, isUnsigned, isBigEndian) and BigInteger.TryWriteBytes(destination, out bytesWritten, isUnsigned, isBigEndian).
- Instead of: Build uint arrays manually or allocate intermediate byte arrays for every conversion.
- Why: Modern implementations use spans, direct copies, and vectorized helpers to reduce allocations and per-byte loops.
- Since .NET 10. Supersedes: .NET 7 span internals reduced allocations; .NET 9 optimized constructors; .NET 10 adds faster non-negative TryWriteBytes.
- Hot path: yes | Complexity: medium
- APIs: `System.Numerics.BigInteger.BigInteger`, `System.Numerics.BigInteger.TryWriteBytes`

## Format primitives into spans instead of strings

Use span-based TryFormat through ISpanFormattable or concrete TryFormat overloads for numbers, enums, DateTime, TimeSpan, Guid, Version, Char, Rune, IntPtr, and UIntPtr.

- Do: Call value.TryFormat(Span<char>, out written, format, provider) or write generic code constrained to System.ISpanFormattable.
- Instead of: Call ToString and then copy or append the resulting string.
- Why: Formatting directly into caller-owned buffers avoids intermediate strings and lets string builders and interpolated string handlers stay allocation-free.
- Since .NET 8. Supersedes: .NET Core 2.1 introduced span formatting; .NET 6 and .NET 8 broadened primitive, enum, and UTF-8 coverage.
- Hot path: either | Complexity: low
- APIs: `System.ISpanFormattable.TryFormat`, `System.Int32.TryFormat`, `System.Enum.TryFormat`, `System.DateTime.TryFormat`, `System.Guid.TryFormat`
- Snippet: [code](../snippets/bcl/numerics.md#format-primitives-into-spans-instead-of-strings)

## Parse UTF-8 primitives without transcoding

Parse UTF-8 numeric primitives and newer primitive-like types directly from ReadOnlySpan<byte>.

- Do: Use IUtf8SpanParsable<TSelf> or concrete Parse/TryParse(ReadOnlySpan<byte>, ...) overloads for numbers and supported types.
- Instead of: Decode UTF-8 to string before parsing.
- Why: Direct UTF-8 parsing avoids temporary UTF-16 buffers and uses the same optimized parsing logic as UTF-16.
- Since .NET 10. Supersedes: .NET 8 added IUtf8SpanParsable for numeric primitives; .NET 10 extends the pattern to Guid, Version, Char, and Rune.
- Hot path: either | Complexity: low
- APIs: `System.IUtf8SpanParsable<TSelf>`, `System.Int32.TryParse`, `System.Guid.Parse`, `System.Guid.TryParse`, `System.Version.Parse`

## Parse primitives from spans instead of substrings

Parse directly from ReadOnlySpan<char> using TryParse and ISpanParsable<TSelf> when slicing larger inputs.

- Do: Call int.TryParse(span, out value), Enum.TryParse<TEnum>(span, out value), DateTime.TryParse(span, out value), or generic T.Parse/T.TryParse with ISpanParsable<T>.
- Instead of: Allocate substrings and then call string-based Parse.
- Why: Span parsing avoids substring allocations and uses the optimized managed primitive parsing paths.
- Since .NET 7. Supersedes: .NET Core 2.1 span parsing and .NET 6 enum span parsing are generalized by .NET 7 ISpanParsable<TSelf>.
- Hot path: either | Complexity: low
- APIs: `System.ISpanParsable<TSelf>`, `System.Int32.TryParse`, `System.Enum.TryParse`, `System.DateTime.TryParse`
- Snippet: [code](../snippets/bcl/numerics.md#parse-primitives-from-spans-instead-of-substrings)

## Use DateTime and TimeSpan fast parse and format paths

Use TryFormat, TryParse, and ParseExact with common invariant formats such as o, r, s, u, and invariant G.

- Do: Call DateTime.TryFormat, DateTimeOffset.TryFormat, TimeSpan.TryFormat, DateTimeOffset.ParseExact, or TryParse on spans with CultureInfo.InvariantCulture when the protocol format is invariant.
- Instead of: Use culture-sensitive default formatting/parsing or allocate strings in protocol and logging hot paths.
- Why: Dedicated DateTime, DateTimeOffset, and TimeSpan paths avoid general formatting overhead, extra culture allocations, and failed-parse boxing.
- Since .NET 9. Supersedes: .NET Core 2.1 and 3.0 optimized o and r; .NET 8 added G, s, u and UTF-8 formatting; .NET 9 removed more parse/format overhead.
- Hot path: either | Complexity: low
- APIs: `System.DateTime.TryFormat`, `System.DateTimeOffset.TryFormat`, `System.TimeSpan.TryFormat`, `System.DateTimeOffset.ParseExact`, `System.Globalization.CultureInfo.InvariantCulture`

## Use DateTime and TimeZoneInfo cached APIs directly

Use DateTime.UtcNow, DateTimeOffset.UtcNow, and TimeZoneInfo APIs directly rather than layering heavier abstractions in hot diagnostics paths.

- Do: Call DateTime.UtcNow, DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById, and TimeZoneInfo.GetSystemTimeZones(skipSorting: true) when sorting is unnecessary.
- Instead of: Cache mutable clones yourself or force sorting when the order is irrelevant.
- Why: Modern implementations cache and streamline common paths, including cross-OS time zone ID lookup and UtcNow.
- Since .NET 8. Supersedes: .NET 6 restored fast UtcNow; .NET 8 improves TimeZoneInfo caching and adds an unsorted GetSystemTimeZones option.
- Hot path: either | Complexity: low
- APIs: `System.DateTime.UtcNow`, `System.DateTimeOffset.UtcNow`, `System.TimeZoneInfo.FindSystemTimeZoneById`, `System.TimeZoneInfo.GetSystemTimeZones`

## Use Enum generic span APIs and TryFormat

Use generic Enum.Parse/TryParse and Enum.TryFormat for enum text conversion.

- Do: Call Enum.TryParse<TEnum>(ReadOnlySpan<char>, out value), Enum.IsDefined<TEnum>(value), and Enum.TryFormat<TEnum>(value, destination, out written).
- Instead of: Use object-based Enum APIs, ToString in hot interpolation, or parse via substrings.
- Why: Generic and span-based enum APIs avoid boxing and substring allocation, and .NET 8+ has optimized enum metadata lookup and interpolation paths.
- Since .NET 8. Supersedes: .NET 6 generic enum APIs and .NET 7 small-enum searches are superseded by .NET 8 typed EnumInfo and TryFormat; .NET 9 further improves non-generic Parse.
- Hot path: either | Complexity: low
- APIs: `System.Enum.TryParse<TEnum>`, `System.Enum.TryFormat<TEnum>`, `System.Enum.IsDefined<TEnum>`, `System.Enum.GetName<TEnum>`

## Use Guid UTF-8 Parse and TryParse

When GUIDs arrive as UTF-8 bytes, parse them with Guid.Parse or Guid.TryParse byte-span overloads.

- Do: Call Guid.Parse(ReadOnlySpan<byte>) or Guid.TryParse(ReadOnlySpan<byte>, out Guid).
- Instead of: Transcode bytes to chars and then call Guid.Parse, or use Utf8Parser when the input is exactly the GUID.
- Why: The .NET 10 Guid UTF-8 parser is faster than transcoding and faster than Utf8Parser for whole-value GUID parsing.
- Since .NET 10. Supersedes: Utf8Parser.TryParse in .NET 8 and earlier is still useful for parsing a prefix from a larger buffer.
- Hot path: either | Complexity: low
- APIs: `System.Guid.Parse`, `System.Guid.TryParse`, `System.IUtf8SpanParsable<System.Guid>`, `System.Buffers.Text.Utf8Parser.TryParse`

## Use HashCode for in-process composite hashing

Use HashCode and HashCode.AddBytes when combining fields or bytes into an in-process hash code.

- Do: Use HashCode.Combine, HashCode.Add, HashCode.AddBytes, and ToHashCode for GetHashCode implementations.
- Instead of: Write ad hoc prime multiplication hash combiners for object hash codes.
- Why: HashCode is optimized for .NET object hashing and includes per-process randomization for hash-table resilience.
- Since .NET 8. Supersedes: Custom deterministic hash loops are still appropriate only when stable cross-process hashes are required.
- Hot path: either | Complexity: low
- APIs: `System.HashCode`, `System.HashCode.Combine`, `System.HashCode.Add`, `System.HashCode.AddBytes`, `System.HashCode.ToHashCode`

## Use Random.GetItems for sampling with replacement

Use GetItems to select many random entries from a caller-supplied choice set.

- Do: Call Random.Shared.GetItems<T>(choices, length) or RandomNumberGenerator.GetItems<T>(choices, length).
- Instead of: Loop over Next(choices.Length) or RandomNumberGenerator.GetInt32 for every output element.
- Why: The framework batches randomness and uses optimized unbiased range selection, especially in .NET 10 for non-power-of-2 choice counts.
- Since .NET 10. Supersedes: .NET 8 introduced GetItems; .NET 9 optimized power-of-2 choices; .NET 10 extends the fast path to non-power-of-2 choices.
- Hot path: either | Complexity: low
- APIs: `System.Random.GetItems`, `System.Security.Cryptography.RandomNumberGenerator.GetItems`

## Use Random.GetString and GetHexString for generated text

Use the built-in random string helpers for generated tokens and test data when cryptographic strength is not required.

- Do: Call Random.Shared.GetString(choices, length) or Random.Shared.GetHexString(length) for pseudo-random text.
- Instead of: Build strings with repeated Random.Next indexing and StringBuilder unless targeting older frameworks.
- Why: The helpers centralize efficient choice sampling and avoid handwritten loops and indexing mistakes.
- Since .NET 10. Supersedes: Manual Random.GetItems plus new string construction for simple random text.
- Hot path: either | Complexity: low
- APIs: `System.Random.GetString`, `System.Random.GetHexString`, `System.Random.Shared`

## Use Random.Shared for casual random values

Use Random.Shared instead of allocating Random instances for sporadic pseudo-random values.

- Do: Call Random.Shared.Next(), NextInt64(), NextSingle(), NextBytes(span), or NextDouble().
- Instead of: Create new Random() each time a random value is needed.
- Why: The shared instance is thread-safe and avoids per-call object allocation and seeding overhead.
- Since .NET 6. Supersedes: Manual thread-local Random caches for common non-cryptographic scenarios.
- Hot path: either | Complexity: low
- APIs: `System.Random.Shared`, `System.Random.Next`, `System.Random.NextBytes`, `System.Random.NextInt64`, `System.Random.NextSingle`

## Use RandomNumberGenerator for cryptographic randomness

Use RandomNumberGenerator APIs for security-sensitive random bytes, numbers, and selections.

- Do: Call RandomNumberGenerator.Fill, GetBytes, GetInt32, GetItems, or GetString-like cryptographic APIs where available.
- Instead of: Use Random or Guid.NewGuid as a security token generator.
- Why: Random is optimized for pseudo-random throughput but is not a cryptographic RNG.
- Since .NET 8. Supersedes: Guid.NewGuid implementation details should not be treated as a general cryptographic random API.
- Hot path: either | Complexity: low
- APIs: `System.Security.Cryptography.RandomNumberGenerator.Fill`, `System.Security.Cryptography.RandomNumberGenerator.GetBytes`, `System.Security.Cryptography.RandomNumberGenerator.GetInt32`, `System.Security.Cryptography.RandomNumberGenerator.GetItems`

## Use System.IO.Hashing for non-cryptographic hashes

Use System.IO.Hashing algorithms such as XxHash3, XxHash128, Crc32, and Crc64 for deterministic non-cryptographic byte hashing.

- Do: Call XxHash3.HashToUInt64, XxHash128.Hash, Crc32.HashToUInt32, or incremental Append/GetCurrentHashAsUInt32 APIs.
- Instead of: Use SHA256 for non-security hashing or maintain a custom FNV/CRC loop for large inputs.
- Why: These implementations are hardware-accelerated or vectorized and are much faster than cryptographic hashes for hash-table, cache-key, and checksum style workloads.
- Since .NET 8. Supersedes: .NET 6 introduced System.IO.Hashing with Crc32, Crc64, XxHash32, and XxHash64; .NET 8 adds XxHash3, XxHash128, numeric return helpers, and faster CRC implementations.
- Hot path: either | Complexity: low
- APIs: `System.IO.Hashing.XxHash3`, `System.IO.Hashing.XxHash128`, `System.IO.Hashing.Crc32`, `System.IO.Hashing.Crc64`, `System.IO.Hashing.NonCryptographicHashAlgorithm`

## Use UTF-8 primitive formatting directly

For UTF-8 output, format numeric primitives, DateTime, DateTimeOffset, and Guid directly into Span<byte>.

- Do: Use IUtf8SpanFormattable.TryFormat or concrete TryFormat(Span<byte>, out bytesWritten, format, provider) overloads.
- Instead of: Format to string or Span<char> and then Encoding.UTF8.GetBytes.
- Why: Direct UTF-8 formatting removes UTF-16 strings and transcoding while sharing the optimized primitive formatting implementation.
- Since .NET 8. Supersedes: Utf8Formatter remains useful for older targets, but primitive IUtf8SpanFormattable is the current direct API in .NET 8+.
- Hot path: either | Complexity: low
- APIs: `System.IUtf8SpanFormattable.TryFormat`, `System.UInt64.TryFormat`, `System.DateTime.TryFormat`, `System.Guid.TryFormat`, `System.Buffers.Text.Utf8Formatter.TryFormat`

## Use Version UTF-8 parsing for protocol bytes

Parse Version values directly from UTF-8 byte spans when the input is already UTF-8.

- Do: Call Version.Parse(ReadOnlySpan<byte>) or Version.TryParse(ReadOnlySpan<byte>, out Version?).
- Instead of: Decode UTF-8 bytes to string solely to call Version.Parse.
- Why: The .NET 10 Version UTF-8 parser avoids transcoding to UTF-16 before parsing.
- Since .NET 10. Supersedes: .NET 6 improved Version.TryFormat and ToString; .NET 10 adds direct UTF-8 parsing.
- Hot path: either | Complexity: low
- APIs: `System.Version.Parse`, `System.Version.TryParse`, `System.IUtf8SpanParsable<System.Version>`

## Use built-in Math and MathF routines

Use Math, MathF, and primitive static math methods for scalar numeric operations such as Round, SinCos, ILogB, Min, Max, Truncate, and decimal arithmetic.

- Do: Call Math.Round, MathF.SinCos, Math.ILogB, decimal operators, Int128/UInt128 operators, and primitive static methods directly.
- Instead of: Maintain custom approximations or manual decompositions for common scalar math unless benchmarked and required.
- Why: Current releases expose optimized managed implementations, JIT intrinsics, constant folding, and hardware-specific code generation.
- Since .NET 10. Supersedes: .NET 7 moved many math operations to efficient managed implementations; .NET 9 improves Round and SinCos constants; .NET 10 improves decimal and UInt128 division.
- Hot path: either | Complexity: low
- APIs: `System.Math.Round`, `System.MathF.SinCos`, `System.Math.ILogB`, `System.Decimal`, `System.UInt128`

## Use numeric binary format specifier

Use the built-in binary format specifier for binary numeric text.

- Do: Use value.ToString("b"), value.TryFormat(destination, out written, "b"), int.Parse(input, NumberStyles.BinaryNumber), or BigInteger.Parse(input, NumberStyles.BinaryNumber).
- Instead of: Manually build binary strings or convert through less direct bases.
- Why: The built-in parser and formatter are optimized and avoid hand-rolled bit-to-character loops.
- Since .NET 9. Supersedes: .NET 8 introduced binary formatting/parsing for fixed-size integer primitives; .NET 9 adds BigInteger binary support.
- Hot path: either | Complexity: low
- APIs: `System.Int32.TryFormat`, `System.Int32.Parse`, `System.Numerics.BigInteger.Parse`, `System.Convert.ToString`

## Use optimized Guid formatting and byte APIs

Use Guid.TryFormat and Guid.TryWriteBytes when producing text or 16-byte representations.

- Do: Call guid.TryFormat(destination, out written, format) and guid.TryWriteBytes(destination).
- Instead of: Call guid.ToString followed by copy, or allocate byte arrays for every conversion.
- Why: Guid formatting and byte conversion paths are vectorized and avoid string or array temporaries.
- Since .NET 8. Supersedes: .NET Core 3.0 span byte conversions and .NET 6 parse improvements are complemented by .NET 8 UTF-8 formatting and .NET 10 X-format cleanup.
- Hot path: either | Complexity: low
- APIs: `System.Guid.TryFormat`, `System.Guid.TryWriteBytes`, `System.Guid.ParseExact`

## Prefer generic math interfaces for reusable numeric algorithms

Write shared numeric algorithms against INumber<TSelf> and related static abstract operator interfaces.

- Do: Constrain methods with System.Numerics.INumber<T>, IBinaryInteger<T>, IFloatingPoint<T>, or narrower operator interfaces and use T.CreateChecked/CreateTruncating where needed.
- Instead of: Maintain separate int, long, float, double, decimal, BigInteger, and Complex code paths when one generic algorithm suffices.
- Why: Generic math removes duplicated per-primitive implementations while preserving efficient specialized code for value types.
- Since .NET 7. Supersedes: .NET 6 preview generic math required experimental references and was not production-supported.
- Hot path: either | Complexity: medium
- APIs: `System.Numerics.INumber<TSelf>`, `System.Numerics.IBinaryInteger<TSelf>`, `System.Numerics.IFloatingPoint<TSelf>`, `System.Numerics.IAdditionOperators<TSelf,TOther,TResult>`
