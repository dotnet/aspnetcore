## ValueStringBuilder

Reach for this in hot formatting paths like query-string construction where stack or pooled char storage avoids StringBuilder allocations.

```diff
- var builder = new StringBuilder();
+ var builder = new ValueStringBuilder(stackalloc char[128]);
  foreach (var pair in parameters)
  {
      builder.Append(first ? '?' : '&');
      first = false;
      builder.Append(UrlEncoder.Default.Encode(pair.Key));
      builder.Append('=');
      builder.Append(UrlEncoder.Default.Encode(pair.Value));
  }
- return builder.ToString();
+ return builder.ToString(); // Invariant: ToString disposes; Dispose on early exit.
```

## PooledArrayBufferWriter<T>

Reach for this when JSON or binary serialization needs an IBufferWriter<T> but repeated array allocations are too expensive.

```csharp
using var bufferWriter = new PooledArrayBufferWriter<byte>();
using (var jsonWriter = new Utf8JsonWriter(bufferWriter))
{
    jsonWriter.WriteStartObject();
    foreach (var (key, value) in values)
    {
        jsonWriter.WriteString(key, value?.ToString());
    }
    jsonWriter.WriteEndObject();
}

Persist(bufferWriter.WrittenMemory); // Invariant: dispose returns the rented array.
```

## CancellationTokenSourcePool

Reach for this in server paths that create many short-lived timeout CTS instances, such as per-response request timeouts.

```csharp
private readonly CancellationTokenSourcePool _ctsPool = new();

public CancellationTokenSource CreateTimeout(HttpContext httpContext, TimeSpan timeout)
{
    var timeoutCts = _ctsPool.Rent();
    try
    {
        timeoutCts.CancelAfter(timeout);
        httpContext.Response.RegisterForDispose(timeoutCts);
        return timeoutCts; // Invariant: Dispose returns the rented CTS to the pool.
    }
    catch
    {
        timeoutCts.Dispose();
        throw;
    }
}
```
