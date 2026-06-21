# Detection signals: which section to consult

Use this to route from what the code is doing to the reference that covers it. The point is progressive disclosure: open a section only when the code under authoring or review touches that area. Match on what the code does, not only on a token; then apply the [decision-framework.md](decision-framework.md) (hot vs cold path, complexity) before acting.

| When the code does this | Consult |
|---|---|
| `Substring`, string `+` or `string.Format` in a loop, `ToUpper`/`ToLower`, `Split`, `Trim`, `PadLeft`, building strings | [bcl-patterns/strings-spans.md](bcl-patterns/strings-spans.md), [language.md](language.md) (interpolation, `u8`) |
| `IndexOf`, `IndexOfAny`, `Contains`, `StartsWith`, `EndsWith`, character scans, `Regex` | [bcl-patterns/searching.md](bcl-patterns/searching.md) |
| `.ToList()`, `.ToArray()`, LINQ (`Where`/`Select`/`Any`/`Count`) in a hot path, `Dictionary`/`HashSet` probes, `new List<T>()` without capacity, `foreach` allocations | [bcl-patterns/collections.md](bcl-patterns/collections.md) |
| `Encoding.`, `Convert.ToBase64String`/`FromBase64String`, hex, `BitConverter`, transcoding | [bcl-patterns/encoding.md](bcl-patterns/encoding.md) |
| `new byte[]`/`new char[]` scratch buffers, `MemoryStream`, `Stream.Read`/`Write`, `ArrayPool`, `IBufferWriter`, `PipeReader`/`PipeWriter` | [bcl-patterns/io.md](bcl-patterns/io.md), [repo-helpers.md](repo-helpers.md) |
| `JsonSerializer`, `JsonConvert`, `Utf8JsonWriter`/`Reader`, XML readers | [bcl-patterns/serialization.md](bcl-patterns/serialization.md) |
| `.ToString()`/`Parse`/`TryParse` on numbers, `DateTime`, `TimeSpan`, `Guid`; formatting | [bcl-patterns/numerics.md](bcl-patterns/numerics.md) |
| `async`/`await`, `Task.Run`, `.Result`/`.Wait()`, returning `Task<T>`, `lock`, `new CancellationTokenSource` | [bcl-patterns/async.md](bcl-patterns/async.md), [repo-helpers.md](repo-helpers.md) |
| `typeof`, `GetMethod`/`GetProperty`, `Activator.CreateInstance`, `MethodInfo.Invoke`, attribute scans | [bcl-patterns/reflection.md](bcl-patterns/reflection.md) |
| `new` in a tight loop, capturing lambda, `params object[]`, value-type to `object` (boxing), `foreach` over `IEnumerable<T>`, lazy property, `Lazy<T>`, `if (x is null) throw` | [language.md](language.md), [bcl-patterns/jit-codegen.md](bcl-patterns/jit-codegen.md), [repo-helpers.md](repo-helpers.md) |
| non-`sealed` internal class, virtual call that could be devirtualized, redundant cast, large struct passed by value | [bcl-patterns/jit-codegen.md](bcl-patterns/jit-codegen.md), [language.md](language.md) |
| `StringBuilder`, `new ArrayBufferWriter<T>()`, raw `new CancellationTokenSource()`, hand-rolled pooling | [repo-helpers.md](repo-helpers.md) (prefer `ValueStringBuilder`, `PooledArrayBufferWriter`, `CancellationTokenSourcePool`) |
| `Encoding`/`SearchValues`/throw helpers/hashing reimplemented locally | [repo-helpers.md](repo-helpers.md) (reuse the shared helper) |
| a struct holding either one value or an array, an accumulator that grows, fixed-size temporary buffers | [language.md](language.md) (small-object optimization, `[InlineArray]`) |
| `stackalloc`, `Span<T>` reinterpret, `MemoryMarshal`, `Unsafe.As`, `CollectionsMarshal` | [language.md](language.md), [bcl-patterns/jit-codegen.md](bcl-patterns/jit-codegen.md) |

Notes:
- A signal is a reason to look, not a verdict. Confirm the code is on a hot path (see the repo hot-path list in [decision-framework.md](decision-framework.md)) before pushing a change.
- One line of code can match several signals. Open each relevant section and pick the highest-leverage, lowest-complexity fix.
- If a match is on a cold path, record it briefly but do not push it.
