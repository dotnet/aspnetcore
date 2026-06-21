## Use built-in LINQ terminal optimizations instead of materializing
Let string.Join enumerate Identity errors directly instead of allocating a List just to join codes.

```diff
  return Succeeded ?
      "Succeeded" :
-     string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
+     string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.Select(error => error.Code)));
```

## Use CollectionsMarshal.AsSpan for controlled List access
When a local List is not mutated during enumeration, iterate its backing span to avoid List indexer overhead.

```diff
+ using System.Runtime.InteropServices;
+
- for (var i = 0; i < pairs.Count; i++)
+ foreach (ref readonly var pair in CollectionsMarshal.AsSpan(pairs))
  {
-     var pair = pairs[i];
      builder.Append(pair.Key).Append(':').Append(pair.Value);
  }
```

## Use FrozenDictionary and FrozenSet for read-mostly data
Build OpenAPI header deny lists once as frozen collections for repeated lookups while generating documents.

```diff
+ using System.Collections.Frozen;
+
- private static readonly HashSet<string> _disallowedHeaderParameters = new(
-     [HeaderNames.Accept, HeaderNames.Authorization, HeaderNames.ContentType],
-     StringComparer.OrdinalIgnoreCase);
+ private static readonly FrozenSet<string> _disallowedHeaderParameters =
+     new[] { HeaderNames.Accept, HeaderNames.Authorization, HeaderNames.ContentType }
+         .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
```

