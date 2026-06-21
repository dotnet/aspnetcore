## Return pooled buffers promptly and safely
For request or cache scratch buffers, rent the temporary byte array and return it as soon as the operation completes.

```diff
- var buffer = new byte[32 * 1024];
- var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
- await destination.WriteAsync(buffer, 0, read, cancellationToken);
+ var buffer = ArrayPool<byte>.Shared.Rent(32 * 1024);
+ try
+ {
+     var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
+     await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
+ }
+ finally
+ {
+     ArrayPool<byte>.Shared.Return(buffer);
+ }
```

## Write to IBufferWriter directly
When a response body exposes a PipeWriter, encode into its span instead of staging bytes in a separate array.

```diff
- var bytes = Encoding.UTF8.GetBytes(message);
- await response.Body.WriteAsync(bytes, cancellationToken);
+ var writer = response.BodyWriter;
+ var span = writer.GetSpan(Encoding.UTF8.GetMaxByteCount(message.Length));
+ var bytesWritten = Encoding.UTF8.GetBytes(message, span);
+ writer.Advance(bytesWritten);
+ await writer.FlushAsync(cancellationToken);
```

## Prefer Memory-based Stream async overloads
Use Memory<byte> and ReadOnlyMemory<byte> overloads so stream code avoids array-offset-count plumbing.

```diff
- var bytesRead = await request.Body.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
- await response.Body.WriteAsync(buffer, 0, bytesRead, cancellationToken);
+ var bytesRead = await request.Body.ReadAsync(buffer.AsMemory(), cancellationToken);
+ await response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
```
