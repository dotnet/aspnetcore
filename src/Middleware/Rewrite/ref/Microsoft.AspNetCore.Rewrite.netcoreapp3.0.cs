// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class RewriteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRewriter(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRewriter(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Rewrite
{
    public static partial class ApacheModRewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddApacheModRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.Extensions.FileProviders.IFileProvider fileProvider, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddApacheModRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.IO.TextReader reader) { throw null; }
    }
    public static partial class IISUrlRewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddIISUrlRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.Extensions.FileProviders.IFileProvider fileProvider, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddIISUrlRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.IO.TextReader reader) { throw null; }
    }
    public partial interface IRule
    {
        void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context);
    }
    public partial class RewriteContext
    {
        public RewriteContext() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.RuleResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider StaticFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RewriteMiddleware
    {
        public RewriteMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Rewrite.RewriteOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class RewriteOptions
    {
        public RewriteOptions() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.IRule> Rules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileProvider StaticFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class RewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions Add(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.AspNetCore.Rewrite.IRule rule) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions Add(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.Action<Microsoft.AspNetCore.Rewrite.RewriteContext> applyRule) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirect(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirect(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode, int? sslPort) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttpsPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWwwPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement, bool skipRemainingRules) { throw null; }
    }
    public enum RuleResult
    {
        ContinueRules = 0,
        EndResponse = 1,
        SkipRemainingRules = 2,
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public partial class BackReferenceCollection
    {
        public BackReferenceCollection(string reference) { }
        public BackReferenceCollection(System.Text.RegularExpressions.GroupCollection references) { }
        public string this[int index] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection references) { }
    }
    public partial class DelegateRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public DelegateRule(System.Action<Microsoft.AspNetCore.Rewrite.RewriteContext> onApplyRule) { }
        public void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class MatchResults
    {
        public static readonly Microsoft.AspNetCore.Rewrite.Internal.MatchResults EmptyFailure;
        public static readonly Microsoft.AspNetCore.Rewrite.Internal.MatchResults EmptySuccess;
        public MatchResults() { }
        public Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection BackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Success { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class ParserContext
    {
        public readonly string Template;
        public ParserContext(string condition) { }
        public char Current { get { throw null; } }
        public int Index { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Back() { throw null; }
        public string Capture() { throw null; }
        public int GetIndex() { throw null; }
        public bool HasNext() { throw null; }
        public void Mark() { }
        public bool Next() { throw null; }
    }
    public partial class Pattern
    {
        public Pattern(System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.PatternSegment> patternSegments) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.PatternSegment> PatternSegments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public abstract partial class PatternSegment
    {
        protected PatternSegment() { }
        public abstract string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences);
    }
    public partial class RedirectRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public RedirectRule(string regex, string replacement, int statusCode) { }
        public System.Text.RegularExpressions.Regex InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Replacement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class RedirectToHttpsRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public RedirectToHttpsRule() { }
        public int? SSLPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class RedirectToWwwRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public readonly int _statusCode;
        public RedirectToWwwRule(int statusCode) { }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class RewriteRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public RewriteRule(string regex, string replacement, bool stopProcessing) { }
        public System.Text.RegularExpressions.Regex InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Replacement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool StopProcessing { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public abstract partial class UrlAction
    {
        protected UrlAction() { }
        protected Microsoft.AspNetCore.Rewrite.Internal.Pattern Url { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public abstract void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences);
    }
    public abstract partial class UrlMatch
    {
        protected UrlMatch() { }
        protected bool Negate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public abstract Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context);
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite
{
    public partial class ApacheModRewriteRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public ApacheModRewriteRule(Microsoft.AspNetCore.Rewrite.Internal.UrlMatch initialMatch, System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Condition> conditions, System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.UrlAction> urlActions) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.UrlAction> Actions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Condition> Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlMatch InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class Condition
    {
        public Condition() { }
        public Microsoft.AspNetCore.Rewrite.Internal.Pattern Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool OrNext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public static partial class ConditionEvaluator
    {
        public static Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Condition> conditions, Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection backReferences) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Condition> conditions, Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection backReferences, bool trackAllCaptures) { throw null; }
    }
    public partial class ConditionPatternParser
    {
        public ConditionPatternParser() { }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ParsedModRewriteInput ParseActionCondition(string condition) { throw null; }
    }
    public enum ConditionType
    {
        IntComp = 3,
        PropertyTest = 1,
        Regex = 0,
        StringComp = 2,
    }
    public partial class CookieActionFactory
    {
        public CookieActionFactory() { }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlActions.ChangeCookieAction Create(string flagValue) { throw null; }
    }
    public partial class FileParser
    {
        public FileParser() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.IRule> Parse(System.IO.TextReader input) { throw null; }
    }
    public partial class FlagParser
    {
        public FlagParser() { }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Flags Parse(string flagString) { throw null; }
    }
    public partial class Flags
    {
        public Flags() { }
        public Flags(System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType, string> flags) { }
        public System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType, string> FlagDictionary { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string this[Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType flag] { get { throw null; } set { } }
        public bool GetValue(Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType flag, out string value) { throw null; }
        public bool HasFlag(Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType flag) { throw null; }
        public void SetFlag(Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.FlagType flag, string value) { }
    }
    public enum FlagType
    {
        Chain = 1,
        Cookie = 2,
        DiscardPath = 3,
        End = 5,
        Env = 4,
        EscapeBackreference = 0,
        Forbidden = 6,
        Gone = 7,
        Handler = 8,
        Last = 9,
        Next = 10,
        NoCase = 11,
        NoEscape = 12,
        NoSubReq = 13,
        NoVary = 14,
        Or = 15,
        PassThrough = 17,
        Proxy = 16,
        QSAppend = 18,
        QSDiscard = 19,
        QSLast = 20,
        Redirect = 21,
        Skip = 22,
        Type = 23,
    }
    public enum OperationType
    {
        Directory = 7,
        Equal = 1,
        Executable = 13,
        ExistingFile = 9,
        ExistingUrl = 12,
        Greater = 2,
        GreaterEqual = 3,
        Less = 4,
        LessEqual = 5,
        None = 0,
        NotEqual = 6,
        RegularFile = 8,
        Size = 11,
        SymbolicLink = 10,
    }
    public partial class ParsedModRewriteInput
    {
        public ParsedModRewriteInput() { }
        public ParsedModRewriteInput(bool invert, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ConditionType conditionType, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.OperationType operationType, string operand) { }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ConditionType ConditionType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Invert { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Operand { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.OperationType OperationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RuleBuilder
    {
        public RuleBuilder() { }
        public void AddAction(Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Flags flags) { }
        public void AddConditionFromParts(Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ParsedModRewriteInput input, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Flags flags) { }
        public void AddMatch(Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ParsedModRewriteInput input, Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.Flags flags) { }
        public void AddRule(string rule) { }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ApacheModRewriteRule Build() { throw null; }
    }
    public partial class RuleRegexParser
    {
        public RuleRegexParser() { }
        public Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite.ParsedModRewriteInput ParseRuleRegex(string regex) { throw null; }
    }
    public enum SegmentType
    {
        ConditionParameter = 2,
        Literal = 0,
        RuleParameter = 3,
        ServerParameter = 1,
    }
    public static partial class ServerVariables
    {
        public static Microsoft.AspNetCore.Rewrite.Internal.PatternSegment FindServerVariable(string serverVariable, Microsoft.AspNetCore.Rewrite.Internal.ParserContext context) { throw null; }
    }
    public partial class TestStringParser
    {
        public TestStringParser() { }
        public Microsoft.AspNetCore.Rewrite.Internal.Pattern Parse(string testString) { throw null; }
    }
    public partial class Tokenizer
    {
        public Tokenizer() { }
        public System.Collections.Generic.IList<string> Tokenize(string rule) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite
{
    public enum ActionType
    {
        AbortRequest = 4,
        CustomResponse = 3,
        None = 0,
        Redirect = 2,
        Rewrite = 1,
    }
    public partial class Condition
    {
        public Condition() { }
        public Microsoft.AspNetCore.Rewrite.Internal.Pattern Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class ConditionCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition>, System.Collections.IEnumerable
    {
        public ConditionCollection() { }
        public ConditionCollection(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.LogicalGrouping grouping, bool trackAllCaptures) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.LogicalGrouping Grouping { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition this[int index] { get { throw null; } }
        public bool TrackAllCaptures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition condition) { }
        public void AddConditions(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition> conditions) { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class ConditionEvaluator
    {
        public static Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.ConditionCollection conditions, Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection backReferences) { throw null; }
    }
    public partial class IISRewriteMap
    {
        public IISRewriteMap(string name) { }
        public string this[string key] { get { throw null; } set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class IISRewriteMapCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMap>, System.Collections.IEnumerable
    {
        public IISRewriteMapCollection() { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMap this[string key] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMap rewriteMap) { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMap> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class IISUrlRewriteRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public IISUrlRewriteRule(string name, Microsoft.AspNetCore.Rewrite.Internal.UrlMatch initialMatch, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.ConditionCollection conditions, Microsoft.AspNetCore.Rewrite.Internal.UrlAction action) { }
        public IISUrlRewriteRule(string name, Microsoft.AspNetCore.Rewrite.Internal.UrlMatch initialMatch, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.ConditionCollection conditions, Microsoft.AspNetCore.Rewrite.Internal.UrlAction action, bool global) { }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlAction Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.ConditionCollection Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Global { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.Internal.UrlMatch InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    public partial class InputParser
    {
        public InputParser() { }
        public InputParser(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMapCollection rewriteMaps) { }
        public Microsoft.AspNetCore.Rewrite.Internal.Pattern ParseInputString(string testString) { throw null; }
        public Microsoft.AspNetCore.Rewrite.Internal.Pattern ParseInputString(string testString, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.UriMatchPart uriMatchPart) { throw null; }
    }
    public partial class InvalidUrlRewriteFormatException : System.FormatException
    {
        public InvalidUrlRewriteFormatException(System.Xml.Linq.XElement element, string message) { }
        public InvalidUrlRewriteFormatException(System.Xml.Linq.XElement element, string message, System.Exception innerException) { }
        public int LineNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int LinePosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public enum LogicalGrouping
    {
        MatchAll = 0,
        MatchAny = 1,
    }
    public enum MatchType
    {
        IsDirectory = 2,
        IsFile = 1,
        Pattern = 0,
    }
    public enum PatternSyntax
    {
        ECMAScript = 0,
        ExactMatch = 2,
        Wildcard = 1,
    }
    public enum RedirectType
    {
        Found = 302,
        Permanent = 301,
        SeeOther = 303,
        Temporary = 307,
    }
    public static partial class RewriteMapParser
    {
        public static Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMapCollection Parse(System.Xml.Linq.XElement xmlRoot) { throw null; }
    }
    public static partial class RewriteTags
    {
        public const string Action = "action";
        public const string Add = "add";
        public const string AppendQueryString = "appendQueryString";
        public const string Conditions = "conditions";
        public const string Enabled = "enabled";
        public const string GlobalRules = "globalRules";
        public const string IgnoreCase = "ignoreCase";
        public const string Input = "input";
        public const string Key = "key";
        public const string LogicalGrouping = "logicalGrouping";
        public const string LogRewrittenUrl = "logRewrittenUrl";
        public const string Match = "match";
        public const string MatchPattern = "matchPattern";
        public const string MatchType = "matchType";
        public const string Name = "name";
        public const string Negate = "negate";
        public const string Pattern = "pattern";
        public const string PatternSyntax = "patternSyntax";
        public const string RedirectType = "redirectType";
        public const string Rewrite = "rewrite";
        public const string RewriteMap = "rewriteMap";
        public const string RewriteMaps = "rewriteMaps";
        public const string Rule = "rule";
        public const string Rules = "rules";
        public const string StatusCode = "statusCode";
        public const string StatusDescription = "statusDescription";
        public const string StatusReason = "statusReason";
        public const string StopProcessing = "stopProcessing";
        public const string SubStatusCode = "subStatusCode";
        public const string TrackAllCaptures = "trackAllCaptures";
        public const string Type = "type";
        public const string Url = "url";
        public const string Value = "value";
    }
    public static partial class ServerVariables
    {
        public static Microsoft.AspNetCore.Rewrite.Internal.PatternSegment FindServerVariable(string serverVariable, Microsoft.AspNetCore.Rewrite.Internal.ParserContext context, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.UriMatchPart uriMatchPart) { throw null; }
    }
    public partial class UriMatchCondition : Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition
    {
        public UriMatchCondition(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.InputParser inputParser, string input, string pattern, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.UriMatchPart uriMatchPart, bool ignoreCase, bool negate) { }
    }
    public enum UriMatchPart
    {
        Full = 0,
        Path = 1,
    }
    public partial class UrlRewriteFileParser
    {
        public UrlRewriteFileParser() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISUrlRewriteRule> Parse(System.IO.TextReader reader) { throw null; }
    }
    public partial class UrlRewriteRuleBuilder
    {
        public UrlRewriteRuleBuilder() { }
        public bool Enabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Global { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.UriMatchPart UriMatchPart { get { throw null; } }
        public void AddUrlAction(Microsoft.AspNetCore.Rewrite.Internal.UrlAction action) { }
        public void AddUrlCondition(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition condition) { }
        public void AddUrlConditions(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.Condition> conditions) { }
        public void AddUrlMatch(string input, bool ignoreCase = true, bool negate = false, Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.PatternSyntax patternSyntax = Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.PatternSyntax.ECMAScript) { }
        public Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISUrlRewriteRule Build() { throw null; }
        public void ConfigureConditionBehavior(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.LogicalGrouping logicalGrouping, bool trackAllCaptures) { }
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal.PatternSegments
{
    public partial class ConditionMatchSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public ConditionMatchSegment(int index) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class DateTimeSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public DateTimeSegment(string segment) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReference) { throw null; }
    }
    public partial class HeaderSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public HeaderSegment(string header) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class IsHttpsModSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public IsHttpsModSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class IsHttpsUrlSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public IsHttpsUrlSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class IsIPV6Segment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public IsIPV6Segment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class LiteralSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public LiteralSegment(string literal) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class LocalAddressSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public LocalAddressSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class LocalPortSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public LocalPortSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class QueryStringSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public QueryStringSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackRefernces, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RemoteAddressSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RemoteAddressSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RemotePortSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RemotePortSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RequestFileNameSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RequestFileNameSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RequestMethodSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RequestMethodSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RewriteMapSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RewriteMapSegment(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.IISRewriteMap rewriteMap, Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class RuleMatchSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public RuleMatchSegment(int index) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class SchemeSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public SchemeSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class ServerProtocolSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public ServerProtocolSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class ToLowerSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public ToLowerSegment(Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class UrlEncodeSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public UrlEncodeSegment(Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    public partial class UrlSegment : Microsoft.AspNetCore.Rewrite.Internal.PatternSegment
    {
        public UrlSegment() { }
        public UrlSegment(Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite.UriMatchPart uriMatchPart) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public partial class AbortAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public AbortAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class ChangeCookieAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public ChangeCookieAction(string name) { }
        public string Domain { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HttpOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan Lifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Secure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class CustomResponseAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public CustomResponseAction(int statusCode) { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string StatusDescription { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string StatusReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class ForbiddenAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public ForbiddenAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class GoneAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public GoneAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class NoneAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public NoneAction(Microsoft.AspNetCore.Rewrite.RuleResult result) { }
        public Microsoft.AspNetCore.Rewrite.RuleResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class RedirectAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public RedirectAction(int statusCode, Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, bool queryStringAppend) { }
        public RedirectAction(int statusCode, Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences) { }
        public bool EscapeBackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringAppend { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringDelete { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
    public partial class RewriteAction : Microsoft.AspNetCore.Rewrite.Internal.UrlAction
    {
        public RewriteAction(Microsoft.AspNetCore.Rewrite.RuleResult result, Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, bool queryStringAppend) { }
        public RewriteAction(Microsoft.AspNetCore.Rewrite.RuleResult result, Microsoft.AspNetCore.Rewrite.Internal.Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences) { }
        public bool EscapeBackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringAppend { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringDelete { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.RuleResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.Internal.BackReferenceCollection conditionBackReferences) { }
    }
}
namespace Microsoft.AspNetCore.Rewrite.Internal.UrlMatches
{
    public partial class ExactMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public ExactMatch(bool ignoreCase, string input, bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public partial class FileSizeMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public FileSizeMatch(bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public partial class IntegerMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public IntegerMatch(int value, Microsoft.AspNetCore.Rewrite.Internal.UrlMatches.IntegerOperationType operation) { }
        public IntegerMatch(string value, Microsoft.AspNetCore.Rewrite.Internal.UrlMatches.IntegerOperationType operation) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public enum IntegerOperationType
    {
        Equal = 0,
        Greater = 1,
        GreaterEqual = 2,
        Less = 3,
        LessEqual = 4,
        NotEqual = 5,
    }
    public partial class IsDirectoryMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public IsDirectoryMatch(bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public partial class IsFileMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public IsFileMatch(bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public partial class RegexMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public RegexMatch(System.Text.RegularExpressions.Regex match, bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public partial class StringMatch : Microsoft.AspNetCore.Rewrite.Internal.UrlMatch
    {
        public StringMatch(string value, Microsoft.AspNetCore.Rewrite.Internal.UrlMatches.StringOperationType operation, bool ignoreCase) { }
        public override Microsoft.AspNetCore.Rewrite.Internal.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    public enum StringOperationType
    {
        Equal = 0,
        Greater = 1,
        GreaterEqual = 2,
        Less = 3,
        LessEqual = 4,
    }
}
