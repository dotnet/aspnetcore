# Performance analyzers and AOT performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## CA1834: append chars to StringBuilder

Use the char overload when appending or inserting a single character with StringBuilder.

- Do: StringBuilder.Append(':') and StringBuilder.Insert(index, ':')
- Instead of: StringBuilder.Append(":") or a const string with length 1
- Why: The char overload avoids single-character string handling and is measurably faster.
- Since .NET 5. Supersedes: single-character string arguments to StringBuilder
- Hot path: yes | Complexity: low
- APIs: `CA1834`, `System.Text.StringBuilder.Append(char)`, `System.Text.StringBuilder.Insert(int,char)`

## CA1835: prefer Memory Stream async overloads

Call Stream.ReadAsync and WriteAsync overloads that accept Memory<byte> or ReadOnlyMemory<byte>.

- Do: stream.ReadAsync(buffer.AsMemory(...)) and stream.WriteAsync(buffer.AsMemory(...))
- Instead of: stream.ReadAsync(byte[], int, int) or stream.WriteAsync(byte[], int, int)
- Why: The newer overloads can complete synchronously without Task allocation and avoid some array pinning costs.
- Since .NET 5. Supersedes: .NET Framework style array-offset-count async Stream APIs on hot paths
- Hot path: yes | Complexity: low
- APIs: `CA1835`, `System.IO.Stream.ReadAsync(System.Memory<byte>,System.Threading.CancellationToken)`, `System.IO.Stream.WriteAsync(System.ReadOnlyMemory<byte>,System.Threading.CancellationToken)`

## CA1845: use span-based string.Concat

Use span-based string.Concat overloads when concatenating slices of existing strings.

- Do: string.Concat(text.AsSpan(start, length), other.AsSpan(...))
- Instead of: string.Concat(text.Substring(start, length), other.Substring(...))
- Why: Passing spans avoids allocating intermediate substrings before producing the final string.
- Since .NET 7. Supersedes: Substring before Concat for string slices
- Hot path: yes | Complexity: low
- APIs: `CA1845`, `System.String.Concat(System.ReadOnlySpan<char>,System.ReadOnlySpan<char>)`, `System.String.AsSpan`

## CA1846: prefer AsSpan over Substring

Pass ReadOnlySpan<char> slices directly to APIs that have span overloads instead of creating substrings.

- Do: value.AsSpan(start, length)
- Instead of: value.Substring(start, length) when the callee accepts ReadOnlySpan<char>
- Why: AsSpan slices are allocation-free while Substring allocates a new string.
- Since .NET 7. Supersedes: Substring as the default way to pass a string slice
- Hot path: yes | Complexity: low
- APIs: `CA1846`, `System.MemoryExtensions.AsSpan(System.String,int,int)`, `System.ReadOnlySpan<char>`

## CA1848: use LoggerMessage logging

Use LoggerMessage delegates or the LoggerMessage source generator for repeated structured logging calls.

- Do: partial methods annotated with [LoggerMessage] or cached LoggerMessage.Define delegates
- Instead of: ILogger.LogInformation with interpolated strings or repeated template parsing on hot paths
- Why: Precompiled logging avoids repeated template parsing, boxing, and object-array allocation when log messages are emitted.
- Since .NET 6. Supersedes: ad hoc logging calls in performance-sensitive framework code
- Hot path: yes | Complexity: low
- APIs: `CA1848`, `Microsoft.Extensions.Logging.LoggerMessageAttribute`, `Microsoft.Extensions.Logging.LoggerMessage.Define`

## CA1853: remove redundant ContainsKey before Remove

Call Remove directly instead of first checking ContainsKey.

- Do: dictionary.Remove(key)
- Instead of: if (dictionary.ContainsKey(key)) dictionary.Remove(key)
- Why: Dictionary.Remove already reports whether the key existed and doing both performs two lookups.
- Since .NET 7. Supersedes: defensive ContainsKey guards before Remove
- Hot path: yes | Complexity: low
- APIs: `CA1853`, `System.Collections.Generic.Dictionary<TKey,TValue>.Remove`

## CA1854: use TryGetValue for lookup plus value

Use TryGetValue when code checks for a key and then reads the value.

- Do: if (dictionary.TryGetValue(key, out var value)) Use(value);
- Instead of: if (dictionary.ContainsKey(key)) Use(dictionary[key]);
- Why: TryGetValue combines key existence and value retrieval into one dictionary lookup instead of two.
- Since .NET 7. Supersedes: ContainsKey plus indexer lookup
- Hot path: yes | Complexity: low
- APIs: `CA1854`, `System.Collections.Generic.Dictionary<TKey,TValue>.TryGetValue`

## CA1858: use StartsWith for prefix checks

Use StartsWith when code checks whether IndexOf returns zero.

- Do: text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
- Instead of: text.IndexOf(prefix, comparison) == 0
- Why: StartsWith only needs to test the prefix while IndexOf may scan the whole string.
- Since .NET 8. Supersedes: IndexOf equality-to-zero prefix checks
- Hot path: yes | Complexity: low
- APIs: `CA1858`, `System.String.StartsWith`

## CA1860: use collection count properties instead of Any

Use Length, Count, or IsEmpty when the target type already exposes an efficient emptiness property.

- Do: array.Length > 0, list.Count > 0, or !queue.IsEmpty
- Instead of: array.Any(), list.Any(), or Count() != 0 on known collection types
- Why: Enumerable.Any may require extension-method dispatch or enumeration, while direct properties are cheaper and clearer.
- Since .NET 8. Supersedes: LINQ Any for known collections with direct count or emptiness members
- Hot path: yes | Complexity: low
- APIs: `CA1860`, `System.Array.Length`, `System.Collections.Generic.ICollection<T>.Count`, `System.Collections.Concurrent.ConcurrentQueue<T>.IsEmpty`

## CA1862: use StringComparison for case-insensitive checks

Use string APIs that accept StringComparison instead of normalizing strings with ToUpper or ToLower before comparing.

- Do: input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
- Instead of: input.ToUpperInvariant().StartsWith(upperPrefix)
- Why: It avoids allocating normalized strings and usually performs much less work.
- Since .NET 8. Supersedes: case normalization before string comparison
- Hot path: yes | Complexity: low
- APIs: `CA1862`, `System.StringComparison`, `System.String.StartsWith(System.String,System.StringComparison)`, `System.String.Equals(System.String,System.StringComparison)`

## CA1864: use TryAdd for add-if-absent

Use TryAdd when code checks key absence and then adds the value.

- Do: dictionary.TryAdd(key, value)
- Instead of: if (!dictionary.ContainsKey(key)) dictionary.Add(key, value)
- Why: TryAdd performs a single dictionary lookup and avoids the duplicate ContainsKey plus Add path.
- Since .NET 8. Supersedes: ContainsKey guard before Add
- Hot path: yes | Complexity: low
- APIs: `CA1864`, `System.Collections.Generic.Dictionary<TKey,TValue>.TryAdd`, `System.Collections.Generic.IDictionary<TKey,TValue>.TryAdd`

## CA1865: use char overloads for one-character strings

Use string method overloads that accept char when the argument is a single-character string and the change is behaviorally equivalent.

- Do: text.IndexOf('v')
- Instead of: text.IndexOf("v")
- Why: Char overloads avoid string-search setup and are much faster for single-character operations.
- Since .NET 8. Supersedes: single-character string arguments to string search APIs
- Hot path: yes | Complexity: low
- APIs: `CA1865`, `System.String.IndexOf(char)`, `System.String.Contains(char)`

## CA1866: use char overloads when comparison semantics stay safe

Convert one-character string arguments to char in string operations when the analyzer determines the overload preserves intended comparison semantics.

- Do: text.StartsWith('@') or text.EndsWith('@') when ordinal char semantics are intended
- Instead of: text.StartsWith("@") for single-character ordinal checks
- Why: The char overload avoids the overhead of string-based search or prefix checks.
- Since .NET 8. Supersedes: single-character string overloads with equivalent char overloads
- Hot path: yes | Complexity: low
- APIs: `CA1866`, `System.String.StartsWith(char)`, `System.String.EndsWith(char)`

## CA1867: review char overload suggestions with culture semantics

Use char overloads for single-character string operations only when any culture-sensitive behavior change is acceptable.

- Do: Use the analyzer fix when ordinal semantics are correct, or keep the string overload when culture-sensitive semantics are required.
- Instead of: blindly replacing culture-sensitive one-character string comparisons with char overloads
- Why: Char overloads are faster, but switching from some string overloads can change linguistic comparison behavior.
- Since .NET 8. Supersedes: treating every single-character string overload conversion as automatically equivalent
- Hot path: yes | Complexity: low
- APIs: `CA1867`, `System.String.StartsWith(char)`, `System.String.EndsWith(char)`, `System.String.IndexOf(char)`

## CA1851: avoid multiple enumerable enumeration

Materialize or restructure code so an IEnumerable<T> is not enumerated multiple times.

- Do: Convert once with ToArray or combine validation and processing in one pass.
- Instead of: foreach over an IEnumerable<T> followed by Count, Any, ToArray, or another foreach over the same sequence
- Why: Repeated enumeration can allocate enumerators, duplicate interface calls, redo work, and observe inconsistent mutable results.
- Since .NET 7. Supersedes: multi-pass LINQ or foreach patterns over unknown enumerables
- Hot path: yes | Complexity: medium
- APIs: `CA1851`, `System.Linq.Enumerable.ToArray<TSource>`, `System.Collections.Generic.IEnumerable<T>`

## Avoid unnecessary static constructors

Prefer field initializers with static readonly data and avoid explicit static constructors unless ordering or side effects require them.

- Do: Use static readonly fields, lazy initialization, and const data where possible.
- Instead of: empty or side-effect-free static constructors and eager heavy static initialization
- Why: Explicit static constructors suppress beforefieldinit and can force type-initialization checks or blocking startup work before simple static field reads.
- Since .NET 5. Supersedes: eager class-constructor patterns used to initialize simple caches
- Hot path: either | Complexity: low
- APIs: `System.Lazy<T>`

## CA1825: use Array.Empty for empty arrays

Replace zero-length array allocations with Array.Empty<T>().

- Do: Array.Empty<T>()
- Instead of: new T[0] or new T[] { }
- Why: Array.Empty<T>() reuses a cached singleton and removes repeated empty array allocations.
- Since .NET 5. Supersedes: manual empty array allocation
- Hot path: either | Complexity: low
- APIs: `CA1825`, `System.Array.Empty<T>`

## CA1850: use one-shot hash APIs

Use static one-shot HashData APIs when hashing data once.

- Do: SHA256.HashData(data)
- Instead of: using var h = SHA256.Create(); h.ComputeHash(data)
- Why: They avoid allocating and disposing a HashAlgorithm instance and are faster for one-shot hashing.
- Since .NET 7. Supersedes: per-call HashAlgorithm.Create plus ComputeHash for one-shot hashing
- Hot path: either | Complexity: low
- APIs: `CA1850`, `System.Security.Cryptography.SHA256.HashData`

## CA1852: seal internal types

Seal private and internal types that have no derived types in the compilation.

- Do: internal sealed class Worker when the type is not meant to be inherited
- Instead of: unsealed non-public classes with no subclasses
- Why: Sealing enables devirtualization, inlining, cheaper type tests, and fewer array or span covariance checks.
- Since .NET 7. Supersedes: leaving all internal helper classes unsealed by default
- Hot path: either | Complexity: low
- APIs: `CA1852`

## CA1859: use concrete types where possible

Use concrete types for locals, fields, and private members when abstraction is not needed.

- Do: List<T> list = new(); for implementation-private data
- Instead of: IList<T> list = new List<T>(); when no abstraction boundary needs IList<T>
- Why: Concrete types allow direct calls, better inlining, and better devirtualization than interface or base-type dispatch.
- Since .NET 8. Supersedes: interface-typed private implementation details by default
- Hot path: either | Complexity: low
- APIs: `CA1859`

## CA1861: cache constant arrays passed as arguments

Lift repeated constant array arguments into cached static readonly fields.

- Do: private static readonly char[] s_separators = [',', ':']; value.Split(s_separators);
- Instead of: value.Split(new[] { ',', ':' }) on repeated paths
- Why: It avoids allocating a new array every time the call site executes.
- Since .NET 8. Supersedes: inline constant new[] arguments in repeated calls
- Hot path: either | Complexity: low
- APIs: `CA1861`

## Use constant ReadOnlySpan lookup tables

Expose small immutable tables as ReadOnlySpan<T> initialized from constant array literals so the compiler can place data in assembly storage or cache it once.

- Do: private static ReadOnlySpan<int> Values => new int[] { 1, 2, 3 };
- Instead of: new T[] literals allocated on each call or mutable static readonly arrays for read-only data
- Why: This avoids per-use array allocation and can enable the JIT to fold or directly load values from static data.
- Since .NET 8. Supersedes: .NET 7 RuntimeHelpers.CreateSpan<T> API without broad compiler support; older static readonly arrays for immutable tables
- Hot path: either | Complexity: low
- APIs: `System.ReadOnlySpan<T>`, `System.Runtime.CompilerServices.RuntimeHelpers.CreateSpan<T>`

## Use InlineArray for embedded fixed buffers

Use InlineArrayAttribute when a struct or object needs a small contiguous fixed-size buffer that can be safely viewed as a Span<T>.

- Do: Define a single-field struct with [InlineArray(n)] and expose Span<T> over it.
- Instead of: heap-allocated helper arrays or unsafe fixed buffers when a safe inline buffer is sufficient
- Why: It keeps storage inline, avoids separate heap arrays, supports managed element types, and avoids unsafe fixed buffers in many cases.
- Since .NET 8. Supersedes: unsafe fixed buffers and stackalloc-only temporary buffers for some embedded buffer scenarios
- Hot path: either | Complexity: medium
- APIs: `System.Runtime.CompilerServices.InlineArrayAttribute`, `System.Span<T>`

## Author for Native AOT and trimming

Avoid unconstrained reflection and dynamic code generation, or annotate the API surface so the trimmer and AOT compiler know what members are required.

- Do: Use source generators and annotate dynamic access with DynamicallyAccessedMembers, RequiresUnreferencedCode, or RequiresDynamicCode as appropriate.
- Instead of: unannotated Type.GetType, Assembly scanning, MakeGenericType, Expression.Compile, or Reflection.Emit on startup paths
- Why: Reflection-heavy code can fail after trimming and prevents Native AOT from removing unused code or compiling predictably.
- Since .NET 7. Supersedes: runtime reflection discovery for serializers, regex, P/Invoke, and logging metadata
- Hot path: either | Complexity: high
- APIs: `System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute`, `System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute`, `System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute`

## Keep module initializers small and nonblocking

Use ModuleInitializerAttribute only for cheap process-wide initialization that must run before any module code executes.

- Do: Put only minimal registration or cache seeding in a [ModuleInitializer] method and move expensive work to lazy paths.
- Instead of: I/O, reflection scans, locks, or large allocations in module initializers
- Why: Module initializers run during module load, so expensive work directly increases startup time and can block Native AOT startup.
- Since .NET 5. Supersedes: eager static constructor work used only to force early initialization
- Hot path: cold | Complexity: medium
- APIs: `System.Runtime.CompilerServices.ModuleInitializerAttribute`
