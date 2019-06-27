# Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite

``` diff
-namespace Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite {
 {
-    public class ApacheModRewriteRule : IRule {
 {
-        public ApacheModRewriteRule(UrlMatch initialMatch, IList<Condition> conditions, IList<UrlAction> urlActions);

-        public IList<UrlAction> Actions { get; }

-        public IList<Condition> Conditions { get; }

-        public UrlMatch InitialMatch { get; }

-        public virtual void ApplyRule(RewriteContext context);

-    }
-    public class Condition {
 {
-        public Condition();

-        public Pattern Input { get; set; }

-        public UrlMatch Match { get; set; }

-        public bool OrNext { get; set; }

-        public MatchResults Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences);

-    }
-    public static class ConditionEvaluator {
 {
-        public static MatchResults Evaluate(IEnumerable<Condition> conditions, RewriteContext context, BackReferenceCollection backReferences);

-        public static MatchResults Evaluate(IEnumerable<Condition> conditions, RewriteContext context, BackReferenceCollection backReferences, bool trackAllCaptures);

-    }
-    public class ConditionPatternParser {
 {
-        public ConditionPatternParser();

-        public ParsedModRewriteInput ParseActionCondition(string condition);

-    }
-    public enum ConditionType {
 {
-        IntComp = 3,

-        PropertyTest = 1,

-        Regex = 0,

-        StringComp = 2,

-    }
-    public class CookieActionFactory {
 {
-        public CookieActionFactory();

-        public ChangeCookieAction Create(string flagValue);

-    }
-    public class FileParser {
 {
-        public FileParser();

-        public IList<IRule> Parse(TextReader input);

-    }
-    public class FlagParser {
 {
-        public FlagParser();

-        public Flags Parse(string flagString);

-    }
-    public class Flags {
 {
-        public Flags();

-        public Flags(IDictionary<FlagType, string> flags);

-        public IDictionary<FlagType, string> FlagDictionary { get; }

-        public string this[FlagType flag] { get; set; }

-        public bool GetValue(FlagType flag, out string value);

-        public bool HasFlag(FlagType flag);

-        public void SetFlag(FlagType flag, string value);

-    }
-    public enum FlagType {
 {
-        Chain = 1,

-        Cookie = 2,

-        DiscardPath = 3,

-        End = 5,

-        Env = 4,

-        EscapeBackreference = 0,

-        Forbidden = 6,

-        Gone = 7,

-        Handler = 8,

-        Last = 9,

-        Next = 10,

-        NoCase = 11,

-        NoEscape = 12,

-        NoSubReq = 13,

-        NoVary = 14,

-        Or = 15,

-        PassThrough = 17,

-        Proxy = 16,

-        QSAppend = 18,

-        QSDiscard = 19,

-        QSLast = 20,

-        Redirect = 21,

-        Skip = 22,

-        Type = 23,

-    }
-    public enum OperationType {
 {
-        Directory = 7,

-        Equal = 1,

-        Executable = 13,

-        ExistingFile = 9,

-        ExistingUrl = 12,

-        Greater = 2,

-        GreaterEqual = 3,

-        Less = 4,

-        LessEqual = 5,

-        None = 0,

-        NotEqual = 6,

-        RegularFile = 8,

-        Size = 11,

-        SymbolicLink = 10,

-    }
-    public class ParsedModRewriteInput {
 {
-        public ParsedModRewriteInput();

-        public ParsedModRewriteInput(bool invert, ConditionType conditionType, OperationType operationType, string operand);

-        public ConditionType ConditionType { get; set; }

-        public bool Invert { get; set; }

-        public string Operand { get; set; }

-        public OperationType OperationType { get; set; }

-    }
-    public class RuleBuilder {
 {
-        public RuleBuilder();

-        public void AddAction(Pattern pattern, Flags flags);

-        public void AddConditionFromParts(Pattern pattern, ParsedModRewriteInput input, Flags flags);

-        public void AddMatch(ParsedModRewriteInput input, Flags flags);

-        public void AddRule(string rule);

-        public ApacheModRewriteRule Build();

-    }
-    public class RuleRegexParser {
 {
-        public RuleRegexParser();

-        public ParsedModRewriteInput ParseRuleRegex(string regex);

-    }
-    public enum SegmentType {
 {
-        ConditionParameter = 2,

-        Literal = 0,

-        RuleParameter = 3,

-        ServerParameter = 1,

-    }
-    public static class ServerVariables {
 {
-        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context);

-    }
-    public class TestStringParser {
 {
-        public TestStringParser();

-        public Pattern Parse(string testString);

-    }
-    public class Tokenizer {
 {
-        public Tokenizer();

-        public IList<string> Tokenize(string rule);

-    }
-}
```

