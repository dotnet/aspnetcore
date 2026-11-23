# Map Web IDL types to .NET

Read this before you design the public types of a family. The examples use the Cookie Store API.
Its Web IDL has a typedef, dictionaries in both directions, members with and without defaults,
overloads, and an enum.

## Names

- Keep Web IDL type names, written in .NET casing. For example, keep `CookieInit` as it is, and
  write `DOMException` as `DomException` and `URL` as `Url`.
- Watch for a name that collides with a common BCL or ASP.NET Core type, such as `Lock`,
  `Request`, or `File`. When one collides, do not rename it on your own. Raise it as an open
  question.

## Value types

- Map `DOMString` and `USVString` to `string`, and their nullable forms to `string?`.
- Map `long` to `int`, `unsigned long` to `uint`, `long long` to `long`, and `unsigned long long` to
  `ulong`. Add `?` for a nullable type.
- Resolve a typedef to the type behind it, and do not create a .NET type for the typedef itself.
  When it is a number, use that number type, even if the name suggests a time or a size. For
  example, `DOMHighResTimeStamp` is a `double`, so map `DOMHighResTimeStamp?` to `double?`.
  `CookieList` is `sequence<CookieListItem>`, so a returned `CookieList` becomes
  `ImmutableArray<CookieListItem>`.
- Map a returned `sequence<T>` or `FrozenArray<T>` to `ImmutableArray<T>`, and a `sequence<T>`
  parameter to `IEnumerable<T>`. Do not use arrays, `List<T>`, or `IReadOnlyList<T>`. Review chose
  `ImmutableArray<T>` because its type says that nobody changes the list, as a JavaScript
  `FrozenArray` does, although most of the repository returns `IReadOnlyList<T>`. Deserialize the
  value as `T[]` and wrap it with `ImmutableCollectionsMarshal.AsImmutableArray(array)`, which does
  not copy it. Initialize an `ImmutableArray<T>` property to `[]`, because enumerating
  `default(ImmutableArray<T>)` throws. Some merged members, such as `CookieStore.GetAllAsync()`,
  still return `IReadOnlyList<T>` until a later change. Do not copy them.
- Do not wrap values in value objects or .NET types such as `DateTimeOffset` or `TimeSpan`.
- JavaScript numbers lose precision above 2^53, so a `long` value can come back changed. Accept
  this, and do not add checks or workarounds for it.

## Dictionaries

- Map a Web IDL dictionary to a `sealed class`. Do not use `record`, because it adds
  compiler-generated members to the public API. The API conventions also forbid a public `record`
  and ask to seal data types.
- Mark a member `required` only when Web IDL marks it `required`. Do not use constructor
  parameters. `required` works with object initializers and JSON serialization as is.
- Give a dictionary that the application passes to the browser `{ get; set; }` properties, like
  other .NET options types. Map its members by the next section.
- Give a dictionary that only the browser returns `{ get; init; }` properties. It is a copy of the
  browser state at that time. Make each member that Web IDL does not mark `required` nullable, such
  as `string?`, even when the browser always fills it in practice. A missing value comes back as
  `null` without an error, so a non-nullable type would hide it. A `required` member that the
  browser omits makes the call throw.

## Members of a dictionary the application passes

Choose the .NET shape of each member from its Web IDL shape.

| Web IDL member | Cookie Store example | .NET property |
| --- | --- | --- |
| `required T x` | `CookieInit.name` | `required T` |
| `T x = value` | `CookieInit.path = "/"`, `sameSite = "strict"`, `partitioned = false` | `T`, initialized to the same value. On a reference type, add `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` |
| `T? x = null` | `CookieInit.domain`, `expires`, `maxAge` | `T?` with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` |
| `T x` with no default | `CookieStoreGetOptions.name`, `url` | `T?` with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` |
| `T? x` with no default, or `T? x` with a default other than `null` | None | Stop and raise it as an open question |

```csharp
public sealed class CookieInit
{
    public required string Name { get; set; }

    public required string Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Expires { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Domain { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Path { get; set; } = "/";

    public CookieSameSite SameSite { get; set; } = CookieSameSite.Strict;

    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    public bool Partitioned { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaxAge { get; set; }
}
```

### Why the table looks like this

- `null` and a missing member mean different things to the browser. Web IDL converts `null` to the
  string `"null"` for a non-nullable string. So `{ path: null }` creates a cookie whose path is
  `"null"`. `getAll({ name: null })` looks for cookies named `"null"`, while `getAll({})` returns
  every cookie. Never let `null` reach the browser for a non-nullable member.
- `JSRuntime` serializes arguments with System.Text.Json and writes a `null` member as `null`,
  because it does not set `DefaultIgnoreCondition`. A `[JsonIgnore]` attribute on the property
  still works, so a member can be left out without JavaScript of your own.
- Do not make every member nullable to mean "not set". A nullable type tells the reader that `null`
  is a valid value. Reading the property back would also show `null` instead of the value the
  browser actually uses.
- Copying a default into .NET has a cost. If the Web IDL default changes later, .NET keeps sending
  the old value. This is accepted. Take each default from the current Web IDL, not from memory.
- The attribute on a reference type with a default is a safety net. If the application assigns
  `null` anyway, the member is left out and the browser uses its default.
- For `T? x = null`, the specification treats `null` and a missing member the same. Leave the
  member out anyway. Chromium crashes the page on `cookieStore.set({ maxAge: null })`, and only an
  E2E test in a real browser shows such a bug. A member that is never sent cannot hit it.
- Initialize an enum member explicitly, as `SameSite` does above, even when `default(T)` gives the
  same value.

## Enums

Map every Web IDL enum to a hand-written `readonly struct` that wraps the string, whether the
application passes it or the browser returns it. A C# `enum` cannot hold a value that a newer
browser adds, so deserializing such a value would fail. With the struct, an unknown value from the
browser reaches the application, and an application can send a value before the package defines
it. Do not use a `record struct`. The API conventions forbid it, because its compiler-generated
members would join the public API without being listed.

Copy `CookieSameSite.cs`. Every enum has the same parts.

- `[JsonConverter(typeof(WebIdlEnumJsonConverter<T>))]`, `IEquatable<T>`, and the internal
  `IWebIdlEnum<T>`, whose explicit `Create(string)` the converter calls.
- A public constructor that takes the string and throws `ArgumentNullException` for `null`.
- One static get-only property per value, named in PascalCase, such as `NoCors` for `"no-cors"`, in
  the order of the Web IDL. When a value has no natural .NET name, such as `""`, raise it as an
  open question.
- A `Value` property with the string, `ToString()` that returns it, and five one-line equality
  members, `Equals(T)`, `Equals(object?)`, `GetHashCode()`, `==`, and `!=`, all on the private
  field. List all of them in `PublicAPI.Unshipped.txt`, with XML docs worded as in
  `CookieSameSite`.

`default(T)` skips every constructor, so the field is `null` in it. Handle it in one of two ways.

- **(a) The Web IDL gives the value a default.** For example, `CookieInit` has
  `CookieSameSite sameSite = "strict"`. Store the default as `null`, so that `default(T)` and the
  default property are equal without a special case, as `CookieSameSite` does.

  ```csharp
  private const string DefaultValue = "strict";

  // The default value is stored as null so that default(CookieSameSite) has the same field as Strict.
  private readonly string? _value;

  public CookieSameSite(string value)
  {
      ArgumentNullException.ThrowIfNull(value);
      _value = value == DefaultValue ? null : value;
  }

  public static CookieSameSite Strict { get; } = new(DefaultValue);

  public string Value => _value ?? DefaultValue;

  public override string ToString() => Value;
  ```

- **(b) The Web IDL gives no default**, for example an enum that the browser only returns. The
  constructor stores the string as it is, so the field is `null` only in `default(T)`. `Value`
  throws for it, but `ToString()` must not, because the debugger, logging, and string
  interpolation call it.

  ```csharp
  // Null only in default(OrientationType); the constructor never stores null.
  private readonly string? _value;

  public string Value => _value ?? throw new InvalidOperationException("The value is uninitialized.");

  public override string ToString() => _value ?? string.Empty;
  ```

- When dictionary members use the same enum with different defaults, or only some of them have a
  default, use (b). For example, `ScrollIntoViewOptions` has `block = "start"` and
  `inline = "nearest"` of one enum. Each dictionary member still initializes its own default in C#.

Test each enum with the serializing test runtime that "Verify" in `SKILL.md` describes.

- Each value sends and reads its exact string, and an unknown string round-trips in both
  directions.
- For (a), `default(T)` equals the default property, and sending it sends the default string, as
  the `CookieSameSite` tests in `CookieStoreTest` do.
- For (b), `default(T).Value` throws `InvalidOperationException`, `default(T).ToString()` returns
  `""`, `default(T)` equals itself and no defined value, and sending it throws
  `InvalidOperationException` before anything reaches the browser.

## Iterables

- Map `iterable<T>` and `iterable<K, V>` to methods that return `IAsyncEnumerable<T>`, named after
  the JavaScript methods, such as `EntriesAsync()`, `KeysAsync()`, and `ValuesAsync()` on
  `UrlSearchParams`. An application then writes `await foreach`. This is a type mapping of the
  iteration protocol, not a .NET substitution.
- Do not port `forEach`. `await ForEachAsync(...)` would return before the callbacks finish, and an
  exception from a callback would never reach the caller. `await foreach` over `EntriesAsync()`
  does the same job without these problems.

## Operation overloads

Web IDL overloads become .NET overloads. Cookie Store has `getAll(USVString name)` and
`getAll(optional CookieStoreGetOptions options = {})`.

```csharp
public ValueTask<ImmutableArray<CookieListItem>> GetAllAsync();
public ValueTask<ImmutableArray<CookieListItem>> GetAllAsync(string name);
public ValueTask<ImmutableArray<CookieListItem>> GetAllAsync(CookieStoreGetOptions options);
```

- Map an optional dictionary parameter whose default is `{}` to an overload without that
  parameter, plus an overload where the parameter is required and non-nullable. Do not write
  `CookieStoreGetOptions? options = null`. The overloads match the call shapes that MDN lists, and
  `null` gets no special meaning. A default value is also compiled into the caller, so it is hard
  to change later.
- Read the method steps in the specification before you add the overload without the parameter.
  Web IDL requires `optional` and `= {}` on a final dictionary parameter without required members,
  so `= {}` does not mean that empty options work. For example, the steps of
  `get(optional CookieStoreGetOptions options = {})` reject empty options with `TypeError`, so
  `get()`, `get({})`, and `get(null)` always fail. Cookie Store has no `GetAsync()` for this
  reason, only `GetAsync(string name)` and `GetAsync(CookieStoreGetOptions options)`.
- When two overloads take a reference type at the same position, a `null` literal does not
  compile, because it matches both. Accept this, and do not add a renamed method or another
  workaround. In Cookie Store, `GetAllAsync(null)` fails this way, but nothing is lost, because
  both parameters are non-nullable and `null` throws `ArgumentNullException` anyway. It matters
  when `null` is a valid value. Web IDL allows `foo(DOMString? value)` next to `foo(Bar bar)` when
  `Bar` is an interface, and JavaScript `foo(null)` calls the first one. In .NET, `Foo(null)` does
  not compile, so the caller writes `Foo(default(string))`.

## Trimming

JS interop serializes dictionaries by reflection, and a trimmed WebAssembly app can break this
without any warning.

- Only the `TValue` of `InvokeAsync` and `GetValueAsync` itself is safe. It carries
  `[DynamicallyAccessedMembers(JsonSerialized)]`, so the trimmer keeps its constructors and
  properties. The annotation does not reach the element type of a collection or the type of a
  property. So when `getAll()` reads a `CookieListItem[]`, the trimmer can remove
  the constructor or the properties of `CookieListItem`, and deserialization breaks.
- An argument is not safe at all. Arguments travel as `object?[]`, which has no annotation. The
  trimmer cannot see that the serializer reads the property getters of `CookieInit`, so it can
  remove them. The member then silently disappears from the JSON.
- `JSRuntime` suppresses the trim warnings for its reflection serializer, so the build stays clean
  in both cases.
- So put `[DynamicDependency(JsonSerialized, typeof(...))]` on each method that passes or receives
  a dictionary through JS interop, for every dictionary type that is not the `TValue` itself.
  Include nested dictionary types, because the attribute does not follow property types.
  `ResourceCollectionProvider.LoadResourceCollection` does this for the elements of
  `ResourceAsset[]`. Take `JsonSerialized` from the shared `LinkerFlags.cs`, which projects include
  with `<Compile Include="$(SharedSourceRoot)LinkerFlags.cs" LinkBase="Shared" />`. The dependency
  lives only while the method lives, so it does not keep unused members of the family.
- The `[JsonConverter]` attribute of an enum struct is also read by reflection.
- Still prove every dictionary member in a trimmed WebAssembly app, not only in unit tests. Send
  each member of a dictionary the application passes, and read each member of a returned one,
  through the E2E tests on the published app.
