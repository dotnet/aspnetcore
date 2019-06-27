# Microsoft.AspNetCore.Rewrite.Internal

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal {
 {
-    public class BackReferenceCollection {
 {
-        public BackReferenceCollection(string reference);

-        public BackReferenceCollection(GroupCollection references);

-        public string this[int index] { get; }

-        public void Add(BackReferenceCollection references);

-    }
-    public class DelegateRule : IRule {
 {
-        public DelegateRule(Action<RewriteContext> onApplyRule);

-        public void ApplyRule(RewriteContext context);

-    }
-    public class MatchResults {
 {
-        public static readonly MatchResults EmptyFailure;

-        public static readonly MatchResults EmptySuccess;

-        public MatchResults();

-        public BackReferenceCollection BackReferences { get; set; }

-        public bool Success { get; set; }

-    }
-    public class ParserContext {
 {
-        public readonly string Template;

-        public ParserContext(string condition);

-        public char Current { get; }

-        public int Index { get; set; }

-        public bool Back();

-        public string Capture();

-        public int GetIndex();

-        public bool HasNext();

-        public void Mark();

-        public bool Next();

-    }
-    public class Pattern {
 {
-        public Pattern(IList<PatternSegment> patternSegments);

-        public IList<PatternSegment> PatternSegments { get; }

-        public string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public abstract class PatternSegment {
 {
-        protected PatternSegment();

-        public abstract string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RedirectRule : IRule {
 {
-        public RedirectRule(string regex, string replacement, int statusCode);

-        public Regex InitialMatch { get; }

-        public string Replacement { get; }

-        public int StatusCode { get; }

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public class RedirectToHttpsRule : IRule {
 {
-        public RedirectToHttpsRule();

-        public Nullable<int> SSLPort { get; set; }

-        public int StatusCode { get; set; }

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public class RedirectToWwwRule : IRule {
 {
-        public readonly int _statusCode;

-        public RedirectToWwwRule(int statusCode);

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public class RewriteRule : IRule {
 {
-        public RewriteRule(string regex, string replacement, bool stopProcessing);

-        public Regex InitialMatch { get; }

-        public string Replacement { get; }

-        public bool StopProcessing { get; }

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public abstract class UrlAction {
 {
-        protected UrlAction();

-        protected Pattern Url { get; set; }

-        public abstract void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public abstract class UrlMatch {
 {
-        protected UrlMatch();

-        protected bool Negate { get; set; }

-        public abstract MatchResults Evaluate(string input, RewriteContext context);

-    }
-}
```

