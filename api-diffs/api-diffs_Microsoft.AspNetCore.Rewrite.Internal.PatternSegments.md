# Microsoft.AspNetCore.Rewrite.Internal.PatternSegments

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments {
 {
-    public class ConditionMatchSegment : PatternSegment {
 {
-        public ConditionMatchSegment(int index);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class DateTimeSegment : PatternSegment {
 {
-        public DateTimeSegment(string segment);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReference);

-    }
-    public class HeaderSegment : PatternSegment {
 {
-        public HeaderSegment(string header);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class IsHttpsModSegment : PatternSegment {
 {
-        public IsHttpsModSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class IsHttpsUrlSegment : PatternSegment {
 {
-        public IsHttpsUrlSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class IsIPV6Segment : PatternSegment {
 {
-        public IsIPV6Segment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class LiteralSegment : PatternSegment {
 {
-        public LiteralSegment(string literal);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class LocalAddressSegment : PatternSegment {
 {
-        public LocalAddressSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class LocalPortSegment : PatternSegment {
 {
-        public LocalPortSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class QueryStringSegment : PatternSegment {
 {
-        public QueryStringSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackRefernces, BackReferenceCollection conditionBackReferences);

-    }
-    public class RemoteAddressSegment : PatternSegment {
 {
-        public RemoteAddressSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RemotePortSegment : PatternSegment {
 {
-        public RemotePortSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RequestFileNameSegment : PatternSegment {
 {
-        public RequestFileNameSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RequestMethodSegment : PatternSegment {
 {
-        public RequestMethodSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RewriteMapSegment : PatternSegment {
 {
-        public RewriteMapSegment(IISRewriteMap rewriteMap, Pattern pattern);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RuleMatchSegment : PatternSegment {
 {
-        public RuleMatchSegment(int index);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class SchemeSegment : PatternSegment {
 {
-        public SchemeSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class ServerProtocolSegment : PatternSegment {
 {
-        public ServerProtocolSegment();

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class ToLowerSegment : PatternSegment {
 {
-        public ToLowerSegment(Pattern pattern);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class UrlEncodeSegment : PatternSegment {
 {
-        public UrlEncodeSegment(Pattern pattern);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class UrlSegment : PatternSegment {
 {
-        public UrlSegment();

-        public UrlSegment(UriMatchPart uriMatchPart);

-        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-}
```

