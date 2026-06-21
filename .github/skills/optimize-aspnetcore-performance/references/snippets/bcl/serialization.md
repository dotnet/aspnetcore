## Use Utf8JsonWriter for direct UTF-8 output
When middleware emits JSON, write UTF-8 directly to the response body instead of building a JSON string first.

```diff
- var json = "{\"enabled\":" + enabled.ToString().ToLowerInvariant() + "}";
- context.Response.ContentType = "application/json";
- await context.Response.WriteAsync(json, cancellationToken);
+ context.Response.ContentType = "application/json";
+ await using var writer = new Utf8JsonWriter(context.Response.BodyWriter);
+ writer.WriteStartObject();
+ writer.WriteBoolean("enabled", enabled);
+ writer.WriteEndObject();
+ await writer.FlushAsync(cancellationToken);
```

## Prefer System.Text.Json source generation over reflection serialization
For known response payloads, use a JsonSerializerContext so serialization can use generated metadata.

```diff
- await JsonSerializer.SerializeAsync(response.Body, problemDetails, JsonOptions, cancellationToken);
+ await JsonSerializer.SerializeAsync(
+     response.Body,
+     problemDetails,
+     ProblemJsonContext.Default.ProblemDetails,
+     cancellationToken);
+
+ [JsonSerializable(typeof(ProblemDetails))]
+ internal sealed partial class ProblemJsonContext : JsonSerializerContext;
```
