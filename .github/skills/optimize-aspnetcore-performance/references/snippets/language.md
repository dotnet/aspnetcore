## BCL throw helpers

In MediaSource constructors, BCL throw helpers keep argument validation compact and inline-friendly.

```diff
 public MediaSource(byte[] data, string mimeType, string cacheKey)
 {
-    if (data is null) throw new ArgumentNullException(nameof(data));
-    if (mimeType is null) throw new ArgumentNullException(nameof(mimeType));
-    if (cacheKey is null) throw new ArgumentNullException(nameof(cacheKey));
+    ArgumentNullException.ThrowIfNull(data);
+    ArgumentNullException.ThrowIfNull(mimeType);
+    ArgumentNullException.ThrowIfNull(cacheKey);
 
     Stream = new MemoryStream(data, writable: false);
 }
```

## sealed for codegen

EndpointMetadataCollection seals a metadata container that is not an inheritance extension point.

```diff
-public class EndpointMetadataCollection : IReadOnlyList<object>
+public sealed class EndpointMetadataCollection : IReadOnlyList<object>
 {
     public static readonly EndpointMetadataCollection Empty =
         new EndpointMetadataCollection(Array.Empty<object>());
 }
```

## small object optimization

KeyValueAccumulator stores one value directly and promotes only when additional values arrive.

```diff
 public void Append(string key, string value)
 {
-    var values = CollectionsMarshal.GetValueRefOrAddDefault(_lists, key, out _);
-    (values ??= new List<string>()).Add(value);
+    if (!_accumulator.TryGetValue(key, out var values))
+    {
+        _accumulator[key] = new StringValues(value);
+    }
+    else if (values.Count == 1)
+    {
+        _accumulator[key] = new[] { values[0]!, value };
+    }
 }
```

## readonly struct

PathString models an immutable HTTP path value as a readonly struct.

```diff
-public struct PathString : IEquatable<PathString>
+public readonly struct PathString : IEquatable<PathString>
 {
     public static readonly PathString Empty = new(string.Empty);
 
     public PathString(string? value)
     {
         Value = value;
     }
 }
```

## struct enumerator

HeaderDictionary exposes a concrete GetEnumerator that returns a nested struct enumerator.

```csharp
public Enumerator GetEnumerator() =>
    Store is null || Store.Count == 0 ? default : new(Store.GetEnumerator());

public struct Enumerator
{
    private Dictionary<string, StringValues>.Enumerator _dictionaryEnumerator;
    private readonly bool _notEmpty;

    internal Enumerator(Dictionary<string, StringValues>.Enumerator enumerator)
    {
        _dictionaryEnumerator = enumerator;
        _notEmpty = true;
    }

    public KeyValuePair<string, StringValues> Current => _dictionaryEnumerator.Current;
    public bool MoveNext() => _notEmpty && _dictionaryEnumerator.MoveNext();
}
```

## reinterpret spans

CircuitId compares string contents as bytes without allocating a converted buffer.

```diff
-    var left = Encoding.Unicode.GetBytes(Secret);
-    var right = Encoding.Unicode.GetBytes(other.Secret);
-    return CryptographicOperations.FixedTimeEquals(left, right);
+    return CryptographicOperations.FixedTimeEquals(
+        MemoryMarshal.AsBytes(Secret.AsSpan()),
+        MemoryMarshal.AsBytes(other.Secret.AsSpan()));
```

## stackalloc skip init

DfaMatcher tokenizes request paths into a bounded stack buffer written before it is read.

```diff
+[SkipLocalsInit]
 public sealed override unsafe Task MatchAsync(HttpContext httpContext)
 {
     ArgumentNullException.ThrowIfNull(httpContext);
 
-    var buffer = new PathSegment[_maxSegmentCount];
+    Span<PathSegment> buffer = stackalloc PathSegment[_maxSegmentCount];
     var count = FastPathTokenizer.Tokenize(path, buffer);
     var segments = buffer.Slice(0, count);
 }
```

## cached boxed values

RoutingMetrics reuses boxed booleans for object-typed metric tags.

```diff
+private static readonly object BoxedTrue = true;
+private static readonly object BoxedFalse = false;
+
 public void MatchSuccess(string route, bool isFallback)
 {
     _matchAttemptsCounter.Add(1,
-        new("aspnetcore.routing.is_fallback", isFallback));
+        new("aspnetcore.routing.is_fallback", isFallback ? BoxedTrue : BoxedFalse));
 }
```

## static lambdas

RouteView uses a static lambda so the cache callback cannot accidentally capture locals.

```diff
 var pageLayoutType = _layoutAttributeCache
-    .GetOrAdd(RouteData.PageType,
-        type => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType)
+    .GetOrAdd(RouteData.PageType,
+        static type => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType)
     ?? DefaultLayout;
```

## ref access to large structs

RenderTreeBuilder updates render-tree frames in place instead of copying them out of the buffer.

```diff
-var parentFrame = _entries.Buffer[parentFrameIndexValue];
+ref var parentFrame = ref _entries.Buffer[parentFrameIndexValue];
 switch (parentFrame.FrameTypeField)
 {
     case RenderTreeFrameType.Element:
         parentFrame.ElementKeyField = value;
         break;
 }
```


