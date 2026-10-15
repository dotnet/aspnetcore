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
- Map `unsigned long` to `uint` and `long long` to `long`. Add `?` for a nullable type.
- Resolve a typedef to the type behind it, and do not create a .NET type for the typedef itself.
  When it is a number, use that number type, even if the name suggests a time or a size. For
  example, `DOMHighResTimeStamp` is a `double`, so map `DOMHighResTimeStamp?` to `double?`.
  `CookieList` is `sequence<CookieListItem>`, so a returned `CookieList` becomes
  `IReadOnlyList<CookieListItem>`.
- Map a returned `sequence<T>` to `IReadOnlyList<T>`, and a `sequence<T>` parameter to
  `IEnumerable<T>`. Do not use arrays or `List<T>`. This follows the API conventions, and newer
  Blazor APIs such as `InputFileChangeEventArgs.GetMultipleFiles()` also return
  `IReadOnlyList<T>`.
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
- Initialize an enum member explicitly, as `SameSite` does above. Do not rely on the first enum
  member being the default value.

## Enums

Map a Web IDL enum to a .NET `enum`. Attach the generic `JsonStringEnumConverter<T>` to the type,
as `Microsoft.AspNetCore.Components.WebAssembly.Authentication.InteractionType` does. The generic
converter is safe for trimming. Give every member `[JsonStringEnumMemberName]` with the exact Web
IDL string.

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<CookieSameSite>))]
public enum CookieSameSite
{
    [JsonStringEnumMemberName("strict")]
    Strict,

    [JsonStringEnumMemberName("lax")]
    Lax,

    [JsonStringEnumMemberName("none")]
    None,
}
```

- Do not rely on a naming policy. The camelCase policy of `JSRuntime` applies to property names
  only, not to enum values. A policy cannot produce values such as `"no-cors"` or `""` either.
- Keep the member order of the Web IDL, and name each member in PascalCase, such as `NoCors` for
  `"no-cors"`. When a value has no natural .NET name, such as `""`, raise it as an open question.
- An enum that the browser returns breaks when a browser adds a new value, because the unknown
  string fails to deserialize. When a family returns an enum, raise this as an open question.

## Operation overloads

Web IDL overloads become .NET overloads. Cookie Store has `getAll(USVString name)` and
`getAll(optional CookieStoreGetOptions options = {})`.

```csharp
public ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync();
public ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync(string name);
public ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync(CookieStoreGetOptions options);
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
  property. So when `getAll()` returns `IReadOnlyList<CookieListItem>`, the trimmer can remove
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
- The enum attributes above are also read by reflection.
- Still prove every dictionary member in a trimmed WebAssembly app, not only in unit tests. Send
  each member of a dictionary the application passes, and read each member of a returned one,
  through the E2E tests on the published app.
