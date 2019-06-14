# Microsoft.AspNetCore.Mvc.Formatters.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.Formatters.Internal {
 {
-    public static class AcceptHeaderParser {
 {
-        public static IList<MediaTypeSegmentWithQuality> ParseAcceptHeader(IList<string> acceptHeaders);

-        public static void ParseAcceptHeader(IList<string> acceptHeaders, IList<MediaTypeSegmentWithQuality> parsedValues);

-    }
-    public enum HttpParseResult {
 {
-        InvalidFormat = 2,

-        NotParsed = 1,

-        Parsed = 0,

-    }
-    public static class HttpTokenParsingRules

-    public interface IFormatFilter : IFilterMetadata {
 {
-        string GetFormat(ActionContext context);

-    }
-    public readonly struct MediaTypeSegmentWithQuality {
 {
-        public MediaTypeSegmentWithQuality(StringSegment mediaType, double quality);

-        public StringSegment MediaType { get; }

-        public double Quality { get; }

-        public override string ToString();

-    }
-}
```

