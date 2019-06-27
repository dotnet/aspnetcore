# Microsoft.AspNetCore.Rewrite.Internal.UrlMatches

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal.UrlMatches {
 {
-    public class ExactMatch : UrlMatch {
 {
-        public ExactMatch(bool ignoreCase, string input, bool negate);

-        public override MatchResults Evaluate(string pattern, RewriteContext context);

-    }
-    public class FileSizeMatch : UrlMatch {
 {
-        public FileSizeMatch(bool negate);

-        public override MatchResults Evaluate(string input, RewriteContext context);

-    }
-    public class IntegerMatch : UrlMatch {
 {
-        public IntegerMatch(int value, IntegerOperationType operation);

-        public IntegerMatch(string value, IntegerOperationType operation);

-        public override MatchResults Evaluate(string input, RewriteContext context);

-    }
-    public enum IntegerOperationType {
 {
-        Equal = 0,

-        Greater = 1,

-        GreaterEqual = 2,

-        Less = 3,

-        LessEqual = 4,

-        NotEqual = 5,

-    }
-    public class IsDirectoryMatch : UrlMatch {
 {
-        public IsDirectoryMatch(bool negate);

-        public override MatchResults Evaluate(string pattern, RewriteContext context);

-    }
-    public class IsFileMatch : UrlMatch {
 {
-        public IsFileMatch(bool negate);

-        public override MatchResults Evaluate(string pattern, RewriteContext context);

-    }
-    public class RegexMatch : UrlMatch {
 {
-        public RegexMatch(Regex match, bool negate);

-        public override MatchResults Evaluate(string pattern, RewriteContext context);

-    }
-    public class StringMatch : UrlMatch {
 {
-        public StringMatch(string value, StringOperationType operation, bool ignoreCase);

-        public override MatchResults Evaluate(string input, RewriteContext context);

-    }
-    public enum StringOperationType {
 {
-        Equal = 0,

-        Greater = 1,

-        GreaterEqual = 2,

-        Less = 3,

-        LessEqual = 4,

-    }
-}
```

