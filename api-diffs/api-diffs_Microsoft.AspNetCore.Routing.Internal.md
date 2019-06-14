# Microsoft.AspNetCore.Routing.Internal

``` diff
 namespace Microsoft.AspNetCore.Routing.Internal {
-    public struct BufferValue {
 {
-        public BufferValue(string value, bool requiresEncoding);

-        public bool RequiresEncoding { get; }

-        public string Value { get; }

-    }
-    public class LinkGenerationDecisionTree {
 {
-        public LinkGenerationDecisionTree(IReadOnlyList<OutboundMatch> entries);

-        public IList<OutboundMatchResult> GetMatches(RouteValueDictionary values, RouteValueDictionary ambientValues);

-    }
-    public struct OutboundMatchResult {
 {
-        public OutboundMatchResult(OutboundMatch match, bool isFallbackMatch);

-        public bool IsFallbackMatch { get; }

-        public OutboundMatch Match { get; }

-    }
-    public struct PathTokenizer : IEnumerable, IEnumerable<StringSegment>, IReadOnlyCollection<StringSegment>, IReadOnlyList<StringSegment> {
 {
-        public PathTokenizer(PathString path);

-        public int Count { get; }

-        public StringSegment this[int index] { get; }

-        public PathTokenizer.Enumerator GetEnumerator();

-        IEnumerator<StringSegment> System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<StringSegment> {
 {
-            public Enumerator(PathTokenizer tokenizer);

-            public StringSegment Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext> {
 {
-        public UriBuilderContextPooledObjectPolicy();

-        public UriBuildingContext Create();

-        public bool Return(UriBuildingContext obj);

-    }
 }
```

