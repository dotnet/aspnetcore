## Prefer SearchValues for repeated byte and char set search
Cache output-cache key delimiters once and reuse the precomputed search strategy at every validation call.

```diff
+ using System.Buffers;
+
+ private static readonly SearchValues<char> CacheKeyDelimiters = SearchValues.Create("=\u001e");
+
  internal static void ThrowIfContainsDelimiters(string? value)
  {
-     if (!string.IsNullOrEmpty(value) && value.AsSpan().IndexOfAny(KeyDelimiter, KeySubDelimiter) >= 0)
+     if (!string.IsNullOrEmpty(value) && value.AsSpan().IndexOfAny(CacheKeyDelimiters) >= 0)
      {
          throw new CacheKeyDelimiterException();
      }
  }
```

## Prefer GeneratedRegex for stable hot regexes
Generate the protobuf timestamp regex at build time instead of constructing a compiled Regex at runtime.

```diff
- internal static class Legacy
+ internal static partial class Legacy
  {
-     private static readonly Regex TimestampRegex = new Regex(@"^(?<datetime>[0-9]{4}-[01][0-9]-[0-3][0-9]T[012][0-9]:[0-5][0-9]:[0-5][0-9])(?<subseconds>\.[0-9]{1,9})?(?<offset>(Z|[+-][0-1][0-9]:[0-5][0-9]))$", RegexOptions.Compiled);
+     [GeneratedRegex(@"^(?<datetime>[0-9]{4}-[01][0-9]-[0-3][0-9]T[012][0-9]:[0-5][0-9]:[0-5][0-9])(?<subseconds>\.[0-9]{1,9})?(?<offset>(Z|[+-][0-1][0-9]:[0-5][0-9]))$")]
+     private static partial Regex TimestampRegex();
  
-     var match = TimestampRegex.Match(value);
+     var match = TimestampRegex().Match(value);
```

