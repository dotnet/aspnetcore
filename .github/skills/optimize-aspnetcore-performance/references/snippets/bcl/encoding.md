## Use Convert span overloads for Base64 input slices
When an ArrayBuilder or pooled buffer already tracks a slice, pass the span directly instead of copying or relying on offset-count overloads.

```diff
- var payload = Convert.ToBase64String(buffer, offset, count);
+ var payload = Convert.ToBase64String(buffer.AsSpan(offset, count));
```

## Use Convert.ToHexString and FromHexString
Use the built-in hex encoder for hash slices instead of formatting then removing separators.

```diff
- var etag = BitConverter.ToString(hash, 0, bytesWritten).Replace("-", "", StringComparison.Ordinal);
+ var etag = Convert.ToHexString(hash.AsSpan(0, bytesWritten));
```

## Use System.Text.Ascii for ASCII-only text
For header names and protocol tokens, validate and transcode ASCII with System.Text.Ascii instead of Encoding.ASCII.

```diff
- var bytes = Encoding.ASCII.GetBytes(headerName);
- if (bytes.Length != headerName.Length)
+ var bytes = new byte[headerName.Length];
+ if (Ascii.FromUtf16(headerName, bytes, out var bytesWritten) != OperationStatus.Done)
  {
      throw new InvalidOperationException("Header name must be ASCII.");
  }
+ bytes = bytes[..bytesWritten];
```


