## Slice spans instead of allocating substrings
Use the span overload when ASP.NET Core only needs to inspect a base-relative URI slice.

```diff
- var candidate = uri.Substring(_baseUri.OriginalString.Length);
- if (candidate.StartsWith("_framework/", StringComparison.Ordinal))
+ var candidate = uri.AsSpan(_baseUri.OriginalString.Length);
+ if (candidate.StartsWith("_framework/", StringComparison.Ordinal))
  {
      return candidate;
  }
```

## Format UTF-16 directly with MemoryExtensions.TryWrite
Write formatted host and port text into a caller-owned buffer instead of allocating a temporary formatted string.

```diff
- var text = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", host, port);
- text.AsSpan().CopyTo(destination);
- charsWritten = text.Length;
- return true;
+ return destination.TryWrite(
+     CultureInfo.InvariantCulture,
+     $"{host}:{port}",
+     out charsWritten);
```

## Use constant ReadOnlySpan<T> data and UTF-8 literals
Keep the multipart boundary prefix as constant UTF-8 data and encode only the dynamic boundary text.

```diff
- _boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary);
+ var byteCount = "\r\n--"u8.Length + Encoding.UTF8.GetByteCount(boundary);
+ _boundaryBytes = new byte[byteCount];
+ "\r\n--"u8.CopyTo(_boundaryBytes);
+ Encoding.UTF8.GetBytes(boundary, _boundaryBytes.AsSpan("\r\n--"u8.Length));
```

## Create final strings directly with string.Create or span Concat
Build the sanitized Blazor field id as the final string instead of routing through StringBuilder.

```diff
- var result = new StringBuilder(fieldName.Length);
- foreach (var c in fieldName)
- {
-     result.Append(InvalidIdChars.Contains(c) ? '_' : c);
- }
- return result.ToString();
+ return string.Create(fieldName.Length, fieldName, static (chars, value) =>
+ {
+     for (var i = 0; i < value.Length; i++)
+     {
+         chars[i] = InvalidIdChars.Contains(value[i]) ? '_' : value[i];
+     }
+ });
```

