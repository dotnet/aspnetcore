# Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite {
 {
-    public enum ActionType {
 {
-        AbortRequest = 4,

-        CustomResponse = 3,

-        None = 0,

-        Redirect = 2,

-        Rewrite = 1,

-    }
-    public class Condition {
 {
-        public Condition();

-        public Pattern Input { get; set; }

-        public UrlMatch Match { get; set; }

-        public MatchResults Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public class ConditionCollection : IEnumerable, IEnumerable<Condition> {
 {
-        public ConditionCollection();

-        public ConditionCollection(LogicalGrouping grouping, bool trackAllCaptures);

-        public int Count { get; }

-        public LogicalGrouping Grouping { get; }

-        public Condition this[int index] { get; }

-        public bool TrackAllCaptures { get; }

-        public void Add(Condition condition);

-        public void AddConditions(IEnumerable<Condition> conditions);

-        public IEnumerator<Condition> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public static class ConditionEvaluator {
 {
-        public static MatchResults Evaluate(ConditionCollection conditions, RewriteContext context, BackReferenceCollection backReferences);

-    }
-    public class IISRewriteMap {
 {
-        public IISRewriteMap(string name);

-        public string Name { get; }

-        public string this[string key] { get; set; }

-    }
-    public class IISRewriteMapCollection : IEnumerable, IEnumerable<IISRewriteMap> {
 {
-        public IISRewriteMapCollection();

-        public int Count { get; }

-        public IISRewriteMap this[string key] { get; }

-        public void Add(IISRewriteMap rewriteMap);

-        public IEnumerator<IISRewriteMap> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class IISUrlRewriteRule : IRule {
 {
-        public IISUrlRewriteRule(string name, UrlMatch initialMatch, ConditionCollection conditions, UrlAction action);

-        public IISUrlRewriteRule(string name, UrlMatch initialMatch, ConditionCollection conditions, UrlAction action, bool global);

-        public UrlAction Action { get; }

-        public ConditionCollection Conditions { get; }

-        public bool Global { get; }

-        public UrlMatch InitialMatch { get; }

-        public string Name { get; }

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public class InputParser {
 {
-        public InputParser();

-        public InputParser(IISRewriteMapCollection rewriteMaps);

-        public Pattern ParseInputString(string testString);

-        public Pattern ParseInputString(string testString, UriMatchPart uriMatchPart);

-    }
-    public class InvalidUrlRewriteFormatException : FormatException {
 {
-        public InvalidUrlRewriteFormatException(XElement element, string message);

-        public InvalidUrlRewriteFormatException(XElement element, string message, Exception innerException);

-        public int LineNumber { get; }

-        public int LinePosition { get; }

-    }
-    public enum LogicalGrouping {
 {
-        MatchAll = 0,

-        MatchAny = 1,

-    }
-    public enum MatchType {
 {
-        IsDirectory = 2,

-        IsFile = 1,

-        Pattern = 0,

-    }
-    public enum PatternSyntax {
 {
-        ECMAScript = 0,

-        ExactMatch = 2,

-        Wildcard = 1,

-    }
-    public enum RedirectType {
 {
-        Found = 302,

-        Permanent = 301,

-        SeeOther = 303,

-        Temporary = 307,

-    }
-    public static class RewriteMapParser {
 {
-        public static IISRewriteMapCollection Parse(XElement xmlRoot);

-    }
-    public static class RewriteTags {
 {
-        public const string Action = "action";

-        public const string Add = "add";

-        public const string AppendQueryString = "appendQueryString";

-        public const string Conditions = "conditions";

-        public const string Enabled = "enabled";

-        public const string GlobalRules = "globalRules";

-        public const string IgnoreCase = "ignoreCase";

-        public const string Input = "input";

-        public const string Key = "key";

-        public const string LogicalGrouping = "logicalGrouping";

-        public const string LogRewrittenUrl = "logRewrittenUrl";

-        public const string Match = "match";

-        public const string MatchPattern = "matchPattern";

-        public const string MatchType = "matchType";

-        public const string Name = "name";

-        public const string Negate = "negate";

-        public const string Pattern = "pattern";

-        public const string PatternSyntax = "patternSyntax";

-        public const string RedirectType = "redirectType";

-        public const string Rewrite = "rewrite";

-        public const string RewriteMap = "rewriteMap";

-        public const string RewriteMaps = "rewriteMaps";

-        public const string Rule = "rule";

-        public const string Rules = "rules";

-        public const string StatusCode = "statusCode";

-        public const string StatusDescription = "statusDescription";

-        public const string StatusReason = "statusReason";

-        public const string StopProcessing = "stopProcessing";

-        public const string SubStatusCode = "subStatusCode";

-        public const string TrackAllCaptures = "trackAllCaptures";

-        public const string Type = "type";

-        public const string Url = "url";

-        public const string Value = "value";

-    }
-    public static class ServerVariables {
 {
-        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context, UriMatchPart uriMatchPart);

-    }
-    public class UriMatchCondition : Condition {
 {
-        public UriMatchCondition(InputParser inputParser, string input, string pattern, UriMatchPart uriMatchPart, bool ignoreCase, bool negate);

-    }
-    public enum UriMatchPart {
 {
-        Full = 0,

-        Path = 1,

-    }
-    public class UrlRewriteFileParser {
 {
-        public UrlRewriteFileParser();

-        public IList<IISUrlRewriteRule> Parse(TextReader reader);

-    }
-    public class UrlRewriteRuleBuilder {
 {
-        public UrlRewriteRuleBuilder();

-        public bool Enabled { get; set; }

-        public bool Global { get; set; }

-        public string Name { get; set; }

-        public UriMatchPart UriMatchPart { get; }

-        public void AddUrlAction(UrlAction action);

-        public void AddUrlCondition(Condition condition);

-        public void AddUrlConditions(IEnumerable<Condition> conditions);

-        public void AddUrlMatch(string input, bool ignoreCase = true, bool negate = false, PatternSyntax patternSyntax = PatternSyntax.ECMAScript);

-        public IISUrlRewriteRule Build();

-        public void ConfigureConditionBehavior(LogicalGrouping logicalGrouping, bool trackAllCaptures);

-    }
-}
```

