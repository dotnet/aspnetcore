# Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite

``` diff
 namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite {
     public class InputParser {
-        public InputParser(IISRewriteMapCollection rewriteMaps);

+        public InputParser(IISRewriteMapCollection rewriteMaps, bool alwaysUseManagedServerVariables);
     }
     public static class ServerVariables {
-        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context, UriMatchPart uriMatchPart);

+        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context, UriMatchPart uriMatchPart, bool alwaysUseManagedServerVariables);
     }
     public class UrlRewriteFileParser {
-        public IList<IISUrlRewriteRule> Parse(TextReader reader);

+        public IList<IISUrlRewriteRule> Parse(TextReader reader, bool alwaysUseManagedServerVariables);
     }
 }
```

