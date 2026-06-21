# Serialization performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Avoid intermediate arrays when JsonNode can consume spans

Feed existing spans into JSON DOM APIs instead of materializing arrays first.

- Do: Use memory.Span with APIs that accept ReadOnlySpan<byte>.
- Instead of: Call Memory<byte>.ToArray() only to pass bytes to a span-accepting JSON API.
- Why: Using Memory<byte>.Span or ReadOnlySpan<byte> avoids potentially large ToArray allocations and copies.
- Since .NET 8. Supersedes: Array materialization used by older JsonNode.To implementation paths.
- Hot path: yes | Complexity: low
- APIs: `System.Memory<T>.Span`, `System.Text.Json.Nodes.JsonNode.ToJsonString`, `System.Text.Json.Nodes.JsonNode.Parse`

## Deserialize multiple top-level JSON values directly

Use built-in topLevelValues support for streams containing consecutive JSON values.

- Do: await foreach over JsonSerializer.DeserializeAsyncEnumerable<T>(stream, topLevelValues: true).
- Instead of: Pre-parse the byte stream into per-object strings or byte arrays before deserializing each value.
- Why: The serializer and reader can consume streaming concatenated JSON directly, avoiding custom pre-parsing and slicing workarounds.
- Since .NET 9. Supersedes: Manual framing for newline-delimited or concatenated JSON values before .NET 9 top-level value support.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable`, `System.Text.Json.Utf8JsonReader`

## Prefer JsonObject TryAdd for conditional adds

Use JsonObject.TryAdd when adding a property only if it is absent.

- Do: obj.TryAdd(propertyName, node);
- Instead of: if (!obj.ContainsKey(name)) obj.Add(name, value);
- Why: The API avoids duplicate key lookups and is faster than ContainsKey followed by Add.
- Since .NET 10. Supersedes: Manual ContainsKey plus Add checks on JsonObject in .NET 9 and earlier.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.Nodes.JsonObject.TryAdd`, `System.Text.Json.Nodes.JsonObject.TryGetPropertyValue`

## Use JsonArray bulk removal APIs

Use JsonArray.RemoveAll or RemoveRange for bulk removals from JSON arrays.

- Do: array.RemoveAll(predicate) or array.RemoveRange(index, count).
- Instead of: Loop with RemoveAt(i) for many matching elements unless removing from the end.
- Why: Bulk removal shifts elements in a linear pattern and avoids accidental O(N^2) behavior from repeated front or middle RemoveAt calls.
- Since .NET 10. Supersedes: Manual JsonArray RemoveAt loops in .NET 9 and earlier.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.Nodes.JsonArray.RemoveAll`, `System.Text.Json.Nodes.JsonArray.RemoveRange`, `System.Text.Json.Nodes.JsonArray.RemoveAt`

## Use generated fast-path metadata for streaming JSON

Pass generated JsonTypeInfo to Stream and async serialization overloads so JsonSerializer can use source-generated fast-path writers.

- Do: JsonSerializer.Serialize(stream, value, MyContext.Default.MyType) or JsonSerializer.SerializeAsync(stream, value, MyContext.Default.MyType).
- Instead of: Streaming with reflection metadata or older combined context paths that lose fast paths.
- Why: In .NET 8 generated fast paths apply to streaming scenarios used by servers, cutting allocations and substantially improving throughput.
- Since .NET 8. Supersedes: .NET 6 and .NET 7 source-generated fast paths that were mainly synchronous and non-streaming.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.JsonSerializer.SerializeAsync`, `System.Text.Json.JsonSerializer.Serialize`, `System.Text.Json.Serialization.Metadata.JsonTypeInfo`

## Use stackalloc spans for small serializer scratch state

For small fixed-size per-object tracking in serializers, use stackalloc Span<T> and fall back to pooled or heap arrays only for larger counts.

- Do: Span<bool> seen = propertyCount <= 32 ? stackalloc bool[propertyCount] : new bool[propertyCount];
- Instead of: Allocate a bool[] for every deserialized object regardless of member count.
- Why: Small stack buffers remove per-object heap arrays and reduce allocation pressure during deserialization.
- Since .NET 7. Supersedes: XmlSerializer generated code before .NET 7 allocated bool[] scratch state per deserialized object.
- Hot path: yes | Complexity: low
- APIs: `System.Span<T>`, `System.Xml.Serialization.XmlSerializer`

## Use string.Create for XML-related formatted strings

Use string.Create with interpolated string handlers when formatting composite XML helper strings with value types.

- Do: string.Create(CultureInfo.InvariantCulture, $"d{value1:d}p{value2:d}");
- Instead of: "d" + value1.ToString("d", culture) + "p" + value2.ToString("d", culture).
- Why: It formats directly into a pooled buffer and avoids temporary ToString allocations before concatenation.
- Since .NET 7. Supersedes: Manual value-type ToString plus concatenation used in older XML formatting code.
- Hot path: yes | Complexity: low
- APIs: `System.String.Create`, `System.Globalization.CultureInfo.InvariantCulture`

## Write JSON to PipeWriter directly

When the destination is a pipeline, serialize directly to PipeWriter overloads instead of adapting through Stream.

- Do: Use JsonSerializer.SerializeAsync(PipeWriter, value, jsonTypeInfo or options).
- Instead of: Wrap a PipeWriter in a Stream adapter just to call Stream-based JsonSerializer overloads.
- Why: Native PipeWriter overloads avoid adapter overhead in pipeline-based servers such as ASP.NET Core.
- Since .NET 9. Supersedes: Older Stream-only serialization paths for pipeline destinations.
- Hot path: yes | Complexity: low
- APIs: `System.Text.Json.JsonSerializer.SerializeAsync`, `System.IO.Pipelines.PipeWriter`

## Stream Base64 JSON values without full buffering

Use Utf8JsonWriter.WriteBase64StringSegment to encode and write binary payload chunks as one JSON string property.

- Do: Write the property name, call WriteBase64StringSegment(chunk, false) for each byte chunk, then call WriteBase64StringSegment(default, true).
- Instead of: Read all bytes, Base64-encode them into a separate buffer, then call WriteBase64String once.
- Why: The writer encodes each input span directly, avoiding a full input buffer and a full intermediate Base64 string or byte array.
- Since .NET 10. Supersedes: .NET 10 WriteStringValueSegment covers general partial strings; WriteBase64StringSegment specializes and supersedes full-buffer Base64 JSON writing.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonWriter.WriteBase64StringSegment`, `System.Text.Json.Utf8JsonWriter.WritePropertyName`

## Stream large JSON string values in segments

Write lazily produced large string property values in multiple Utf8JsonWriter segments.

- Do: Call Utf8JsonWriter.WriteStringValueSegment repeatedly and mark the final segment when the value is complete.
- Instead of: Accumulate the complete string value in memory and write it in one call.
- Why: Segmented writing reduces latency and working set by avoiding buffering the entire string value before writing JSON.
- Since .NET 10. Supersedes: Older Utf8JsonWriter string APIs that required a complete string value per property.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonWriter.WriteStringValueSegment`

## Use JsonMarshal raw UTF-8 spans for raw JSON text

Use JsonMarshal.GetRawUtf8Value and GetRawUtf8PropertyName when raw JsonElement text or property names are needed as UTF-8 bytes.

- Do: Call JsonMarshal.GetRawUtf8Value(element) or JsonMarshal.GetRawUtf8PropertyName(property) while the backing JsonDocument remains alive.
- Instead of: Call GetRawText or Name just to re-encode the result to UTF-8.
- Why: Raw span access avoids allocating and transcoding strings produced by JsonElement.GetRawText or JsonProperty.Name.
- Since .NET 10. Supersedes: .NET 9 introduced GetRawUtf8Value; .NET 10 extends the same allocation-free raw access to property names.
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.InteropServices.JsonMarshal.GetRawUtf8Value`, `System.Runtime.InteropServices.JsonMarshal.GetRawUtf8PropertyName`, `System.Text.Json.JsonElement.GetRawText`

## Use Utf8JsonReader for direct UTF-8 parsing

Parse UTF-8 JSON with Utf8JsonReader when hand-written protocol parsing or selective reading is needed.

- Do: Read tokens from Utf8JsonReader and consume ValueSpan, ValueSequence, ValueTextEquals, CopyString, or ValueIsEscaped as appropriate.
- Instead of: Convert UTF-8 payloads to string and then parse or compare string values unnecessarily.
- Why: The reader works over bytes and spans, avoiding full object materialization and string transcoding for skipped or selectively consumed data.
- Since .NET 7. Supersedes: Earlier reader usage that allocated strings to inspect values when CopyString and ValueIsEscaped can avoid it.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonReader`, `System.Text.Json.Utf8JsonReader.CopyString`, `System.Text.Json.Utf8JsonReader.ValueIsEscaped`, `System.Text.Json.Utf8JsonReader.ValueTextEquals`

## Use Utf8JsonWriter for direct UTF-8 output

Use Utf8JsonWriter to write JSON tokens directly to Stream, IBufferWriter<byte>, or PipeWriter-backed output.

- Do: Construct Utf8JsonWriter over the final destination and call WriteStartObject, WriteString, WriteNumber, WriteEndObject, and Flush or FlushAsync.
- Instead of: Build JSON with string concatenation, StringBuilder, or SerializeToString followed by Encoding.UTF8.GetBytes.
- Why: Direct UTF-8 writing skips intermediate strings, transcoding, and extra byte arrays while giving precise control over hot serialization paths.
- Since .NET Core 3.0. Supersedes: String-based JSON construction before System.Text.Json became the recommended JSON stack.
- Hot path: yes | Complexity: medium
- APIs: `System.Text.Json.Utf8JsonWriter`, `System.Buffers.IBufferWriter<T>`, `System.Text.Json.Utf8JsonWriter.WriteString`, `System.Text.Json.Utf8JsonWriter.WriteNumber`
- Snippet: [code](../snippets/bcl/serialization.md#use-utf8jsonwriter-for-direct-utf-8-output)

## Cache and reuse JsonSerializerOptions

Create configured JsonSerializerOptions once and reuse it across serialization and deserialization calls.

- Do: Keep a static, singleton, or DI-managed JsonSerializerOptions instance, or use JsonSerializerOptions.Default when default settings are enough.
- Instead of: new JsonSerializerOptions { ... } on every Serialize or Deserialize call.
- Why: Options instances hold serializer metadata caches; recreating them repeatedly adds avoidable allocation and metadata lookup or generation overhead even after .NET 7 mitigations.
- Since .NET 8. Supersedes: .NET 7 global metadata cache reduced but did not remove the cost of per-call options allocation; CA1869 in .NET 8 makes reuse explicit.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.JsonSerializerOptions`, `System.Text.Json.JsonSerializerOptions.Default`

## Prefer System.Text.Json source generation over reflection serialization

Use a JsonSerializerContext and generated JsonTypeInfo as the primary serialization path for known DTOs.

- Do: Annotate a partial JsonSerializerContext with JsonSerializableAttribute and pass MyContext.Default.MyType or JsonTypeInfo to JsonSerializer.
- Instead of: Relying on reflection-based JsonSerializer.Serialize or Deserialize for hot known types.
- Why: Build-time metadata and fast-path writers avoid runtime reflection, reflection emit, startup cost, working set, and Native AOT trimming problems.
- Since .NET 8. Supersedes: .NET 5 and earlier reflection/reflection-emit metadata caching as the primary hot-path strategy; .NET 6 introduced source generation and .NET 8 made generated fast paths work for streaming.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.Serialization.JsonSerializerContext`, `System.Text.Json.Serialization.JsonSerializableAttribute`, `System.Text.Json.Serialization.Metadata.JsonTypeInfo`, `System.Text.Json.JsonSerializer.Serialize`
- Snippet: [code](../snippets/bcl/serialization.md#prefer-systemtextjson-source-generation-over-reflection-serialization)

## Scope JsonDocument lifetime and dispose it

For temporary DOM access, parse a JsonDocument, use RootElement only inside the scope, and dispose the document promptly.

- Do: using JsonDocument doc = JsonDocument.Parse(json); consume doc.RootElement inside the using block.
- Instead of: Return JsonDocument.Parse(json).RootElement or keep spans from a disposed document.
- Why: JsonDocument may hold ArrayPool buffers, and failing to dispose or returning RootElement directly can leak valuable pooled arrays or corrupt raw spans after reuse.
- Since .NET Core 3.0. Supersedes: Detached-element workarounds are superseded by JsonElement.Parse in .NET 10 when scoped use is not possible.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.JsonDocument`, `System.Text.Json.JsonDocument.Parse`, `System.Text.Json.JsonDocument.RootElement`, `System.IDisposable.Dispose`

## Use JsonElement.Parse for detached elements

Use JsonElement.Parse when a JsonElement must be returned or stored beyond a local JsonDocument scope.

- Do: return JsonElement.Parse(json); for detached element ownership.
- Instead of: Return JsonDocument.Parse(json).RootElement or parse, clone, and dispose unless scoped document use is enough.
- Why: It avoids the clone overhead of JsonDocument.RootElement.Clone and the extra JsonSerializer machinery of Deserialize<JsonElement> while preventing ArrayPool lifetime bugs.
- Since .NET 10. Supersedes: JsonDocument.Parse plus RootElement.Clone and JsonSerializer.Deserialize<JsonElement> workarounds from .NET 9 and earlier.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.JsonElement.Parse`, `System.Text.Json.JsonDocument.Parse`, `System.Text.Json.JsonElement.Clone`

## Use span-based enum JSON conversion paths

Use System.Text.Json enum converters and naming support rather than custom string-heavy enum parsing.

- Do: Configure JsonStringEnumConverter or JsonStringEnumConverter<TEnum> and JsonEnumMemberNameAttribute where needed.
- Instead of: Custom converters that allocate strings or dictionaries per enum parse.
- Why: .NET 9 enum handling uses allocation-free span lookup paths and avoids allocations for common string enum serialization.
- Since .NET 9. Supersedes: Older enum converter paths that allocated during string enum serialization.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.Serialization.JsonStringEnumConverter`, `System.Text.Json.Serialization.JsonStringEnumConverter<TEnum>`, `System.Text.Json.Serialization.JsonEnumMemberNameAttribute`

## Use XmlReader and XmlWriter streaming APIs for XML

Process XML with XmlReader and XmlWriter over streams when documents can be handled incrementally.

- Do: Create XmlReader or XmlWriter over a Stream and read or write nodes incrementally; set XmlReaderSettings.Async when using ReadAsync.
- Instead of: Load or build the whole XML document as strings when only sequential processing is required.
- Why: Streaming avoids constructing large DOMs or full strings and benefits from allocation reductions in modern System.Xml paths.
- Since .NET 7. Supersedes: Older async XmlReader paths had larger LOH-sized buffers; .NET 7 reduced that overhead, but streaming remains the author-controlled pattern.
- Hot path: either | Complexity: medium
- APIs: `System.Xml.XmlReader`, `System.Xml.XmlReader.Create`, `System.Xml.XmlReader.ReadAsync`, `System.Xml.XmlWriter`, `System.Xml.XmlWriter.Create`
