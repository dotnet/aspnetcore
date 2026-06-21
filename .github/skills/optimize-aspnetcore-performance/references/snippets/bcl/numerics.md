## Parse primitives from spans instead of substrings
When parsing route or protocol slices, TryParse the span and avoid substring allocation plus exception-driven control flow.

```diff
- var tenantId = int.Parse(segment.Substring(0, slash), CultureInfo.InvariantCulture);
- var page = int.Parse(segment.Substring(slash + 1), CultureInfo.InvariantCulture);
+ if (!int.TryParse(segment.AsSpan(0, slash), CultureInfo.InvariantCulture, out var tenantId) ||
+     !int.TryParse(segment.AsSpan(slash + 1), CultureInfo.InvariantCulture, out var page))
+ {
+     return Results.BadRequest();
+ }
```

## Format primitives into spans instead of strings
For small protocol values, format into a writer span before flushing instead of allocating a temporary string.

```diff
- var value = statusCode.ToString(CultureInfo.InvariantCulture);
- await response.WriteAsync(value, cancellationToken);
+ var span = response.BodyWriter.GetSpan(3);
+ if (!statusCode.TryFormat(span, out var bytesWritten, provider: CultureInfo.InvariantCulture))
+ {
+     return Results.StatusCode(statusCode);
+ }
+ response.BodyWriter.Advance(bytesWritten);
+ await response.BodyWriter.FlushAsync(cancellationToken);
```
