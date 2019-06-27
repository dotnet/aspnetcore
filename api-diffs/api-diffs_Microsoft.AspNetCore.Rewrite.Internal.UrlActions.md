# Microsoft.AspNetCore.Rewrite.Internal.UrlActions

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions {
 {
-    public class AbortAction : UrlAction {
 {
-        public AbortAction();

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class ChangeCookieAction : UrlAction {
 {
-        public ChangeCookieAction(string name);

-        public string Domain { get; set; }

-        public bool HttpOnly { get; set; }

-        public TimeSpan Lifetime { get; set; }

-        public string Name { get; }

-        public string Path { get; set; }

-        public bool Secure { get; set; }

-        public string Value { get; set; }

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class CustomResponseAction : UrlAction {
 {
-        public CustomResponseAction(int statusCode);

-        public int StatusCode { get; }

-        public string StatusDescription { get; set; }

-        public string StatusReason { get; set; }

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class ForbiddenAction : UrlAction {
 {
-        public ForbiddenAction();

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class GoneAction : UrlAction {
 {
-        public GoneAction();

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class NoneAction : UrlAction {
 {
-        public NoneAction(RuleResult result);

-        public RuleResult Result { get; }

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RedirectAction : UrlAction {
 {
-        public RedirectAction(int statusCode, Pattern pattern, bool queryStringAppend);

-        public RedirectAction(int statusCode, Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences);

-        public bool EscapeBackReferences { get; }

-        public bool QueryStringAppend { get; }

-        public bool QueryStringDelete { get; }

-        public int StatusCode { get; }

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class RewriteAction : UrlAction {
 {
-        public RewriteAction(RuleResult result, Pattern pattern, bool queryStringAppend);

-        public RewriteAction(RuleResult result, Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences);

-        public bool EscapeBackReferences { get; }

-        public bool QueryStringAppend { get; }

-        public bool QueryStringDelete { get; }

-        public RuleResult Result { get; }

-        public override void ApplyAction(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-}
```

