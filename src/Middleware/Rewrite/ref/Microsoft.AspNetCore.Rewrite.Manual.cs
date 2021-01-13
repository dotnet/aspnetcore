// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    internal partial class BackReferenceCollection
    {
        public BackReferenceCollection(string reference) { }
        public BackReferenceCollection(System.Text.RegularExpressions.GroupCollection references) { }
        public string this[int index] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.BackReferenceCollection references) { }
    }
    internal partial class MatchResults
    {
        public static readonly Microsoft.AspNetCore.Rewrite.MatchResults EmptyFailure;
        public static readonly Microsoft.AspNetCore.Rewrite.MatchResults EmptySuccess;
        public MatchResults() { }
        public Microsoft.AspNetCore.Rewrite.BackReferenceCollection BackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Success { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ParserContext
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
    internal partial class Pattern
    {
        public Pattern(System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.PatternSegment> patternSegments) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.PatternSegment> PatternSegments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal abstract partial class PatternSegment
    {
        protected PatternSegment() { }
        public abstract string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences);
    }
    internal static partial class Resources
    {
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string Error_ChangeEnvironmentNotSupported { get { throw null; } }
        internal static string Error_CouldNotParseInteger { get { throw null; } }
        internal static string Error_InputParserIndexOutOfRange { get { throw null; } }
        internal static string Error_InputParserInvalidInteger { get { throw null; } }
        internal static string Error_InputParserMissingCloseBrace { get { throw null; } }
        internal static string Error_InputParserNoBackreference { get { throw null; } }
        internal static string Error_InputParserUnrecognizedParameter { get { throw null; } }
        internal static string Error_IntegerMatch_FormatExceptionMessage { get { throw null; } }
        internal static string Error_InvalidChangeCookieFlag { get { throw null; } }
        internal static string Error_ModRewriteGeneralParseError { get { throw null; } }
        internal static string Error_ModRewriteParseError { get { throw null; } }
        internal static string Error_UnsupportedServerVariable { get { throw null; } }
        internal static string Error_UrlRewriteParseError { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string FormatError_CouldNotParseInteger(object p0) { throw null; }
        internal static string FormatError_InputParserIndexOutOfRange(object p0, object p1) { throw null; }
        internal static string FormatError_InputParserInvalidInteger(object p0, object p1) { throw null; }
        internal static string FormatError_InputParserMissingCloseBrace(object p0) { throw null; }
        internal static string FormatError_InputParserNoBackreference(object p0) { throw null; }
        internal static string FormatError_InputParserUnrecognizedParameter(object p0, object p1) { throw null; }
        internal static string FormatError_InvalidChangeCookieFlag(object p0) { throw null; }
        internal static string FormatError_ModRewriteGeneralParseError(object p0) { throw null; }
        internal static string FormatError_ModRewriteParseError(object p0, object p1) { throw null; }
        internal static string FormatError_UnsupportedServerVariable(object p0) { throw null; }
        internal static string FormatError_UrlRewriteParseError(object p0, object p1, object p2) { throw null; }
    }
    internal abstract partial class UrlAction
    {
        protected UrlAction() { }
        protected Microsoft.AspNetCore.Rewrite.Pattern Url { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public abstract void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences);
    }
    internal abstract partial class UrlMatch
    {
        protected UrlMatch() { }
        protected bool Negate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public abstract Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context);
    }
}

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal partial class ApacheModRewriteRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public ApacheModRewriteRule(Microsoft.AspNetCore.Rewrite.UrlMatch initialMatch, System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Condition> conditions, System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.UrlAction> urlActions) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.UrlAction> Actions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Condition> Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.UrlMatch InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    internal partial class Condition
    {
        public Condition() { }
        public Microsoft.AspNetCore.Rewrite.Pattern Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.UrlMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool OrNext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class ConditionPatternParser
    {
        public ConditionPatternParser() { }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ParsedModRewriteInput ParseActionCondition(string condition) { throw null; }
    }
    internal enum ConditionType
    {
        Regex = 0,
        PropertyTest = 1,
        StringComp = 2,
        IntComp = 3,
    }
    internal partial class CookieActionFactory
    {
        public CookieActionFactory() { }
        public Microsoft.AspNetCore.Rewrite.UrlActions.ChangeCookieAction Create(string flagValue) { throw null; }
    }
    internal partial class FileParser
    {
        public FileParser() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.IRule> Parse(System.IO.TextReader input) { throw null; }
    }
    internal partial class FlagParser
    {
        public FlagParser() { }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Flags Parse(string flagString) { throw null; }
    }
    internal partial class Flags
    {
        public Flags() { }
        public Flags(System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType, string> flags) { }
        public System.Collections.Generic.IDictionary<Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType, string> FlagDictionary { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string this[Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType flag] { get { throw null; } set { } }
        public bool GetValue(Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType flag, out string value) { throw null; }
        public bool HasFlag(Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType flag) { throw null; }
        public void SetFlag(Microsoft.AspNetCore.Rewrite.ApacheModRewrite.FlagType flag, string value) { }
    }
    internal enum FlagType
    {
        EscapeBackreference = 0,
        Chain = 1,
        Cookie = 2,
        DiscardPath = 3,
        Env = 4,
        End = 5,
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
        Proxy = 16,
        PassThrough = 17,
        QSAppend = 18,
        QSDiscard = 19,
        QSLast = 20,
        Redirect = 21,
        Skip = 22,
        Type = 23,
    }
    internal enum OperationType
    {
        None = 0,
        Equal = 1,
        Greater = 2,
        GreaterEqual = 3,
        Less = 4,
        LessEqual = 5,
        NotEqual = 6,
        Directory = 7,
        RegularFile = 8,
        ExistingFile = 9,
        SymbolicLink = 10,
        Size = 11,
        ExistingUrl = 12,
        Executable = 13,
    }
    internal partial class ParsedModRewriteInput
    {
        public ParsedModRewriteInput() { }
        public ParsedModRewriteInput(bool invert, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ConditionType conditionType, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.OperationType operationType, string operand) { }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ConditionType ConditionType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Invert { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Operand { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.OperationType OperationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class RuleBuilder
    {
        internal System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.UrlAction> _actions;
        public RuleBuilder() { }
        public void AddAction(Microsoft.AspNetCore.Rewrite.Pattern pattern, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Flags flags) { }
        public void AddConditionFromParts(Microsoft.AspNetCore.Rewrite.Pattern pattern, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ParsedModRewriteInput input, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Flags flags) { }
        public void AddMatch(Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ParsedModRewriteInput input, Microsoft.AspNetCore.Rewrite.ApacheModRewrite.Flags flags) { }
        public void AddRule(string rule) { }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ApacheModRewriteRule Build() { throw null; }
    }
    internal partial class RuleRegexParser
    {
        public RuleRegexParser() { }
        public Microsoft.AspNetCore.Rewrite.ApacheModRewrite.ParsedModRewriteInput ParseRuleRegex(string regex) { throw null; }
    }
    internal partial class TestStringParser
    {
        public TestStringParser() { }
        public Microsoft.AspNetCore.Rewrite.Pattern Parse(string testString) { throw null; }
    }
    internal partial class Tokenizer
    {
        public Tokenizer() { }
        public System.Collections.Generic.IList<string> Tokenize(string rule) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal enum ActionType
    {
        None = 0,
        Rewrite = 1,
        Redirect = 2,
        CustomResponse = 3,
        AbortRequest = 4,
    }
    internal partial class Condition
    {
        public Condition() { }
        public Microsoft.AspNetCore.Rewrite.Pattern Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.UrlMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class ConditionCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition>, System.Collections.IEnumerable
    {
        public ConditionCollection() { }
        public ConditionCollection(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.LogicalGrouping grouping, bool trackAllCaptures) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.LogicalGrouping Grouping { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition this[int index] { get { throw null; } }
        public bool TrackAllCaptures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition condition) { }
        public void AddConditions(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition> conditions) { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    internal partial class IISRewriteMap
    {
        public IISRewriteMap(string name) { }
        public string this[string key] { get { throw null; } set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class IISRewriteMapCollection : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMap>, System.Collections.IEnumerable
    {
        public IISRewriteMapCollection() { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMap this[string key] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMap rewriteMap) { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMap> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    internal partial class IISUrlRewriteRule : Microsoft.AspNetCore.Rewrite.IRule
    {
        public IISUrlRewriteRule(string name, Microsoft.AspNetCore.Rewrite.UrlMatch initialMatch, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.ConditionCollection conditions, Microsoft.AspNetCore.Rewrite.UrlAction action) { }
        public IISUrlRewriteRule(string name, Microsoft.AspNetCore.Rewrite.UrlMatch initialMatch, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.ConditionCollection conditions, Microsoft.AspNetCore.Rewrite.UrlAction action, bool global) { }
        public Microsoft.AspNetCore.Rewrite.UrlAction Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.ConditionCollection Conditions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Global { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.UrlMatch InitialMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context) { }
    }
    internal partial class InputParser
    {
        public InputParser() { }
        public InputParser(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMapCollection rewriteMaps, bool alwaysUseManagedServerVariables) { }
        public Microsoft.AspNetCore.Rewrite.Pattern ParseInputString(string testString) { throw null; }
        public Microsoft.AspNetCore.Rewrite.Pattern ParseInputString(string testString, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UriMatchPart uriMatchPart) { throw null; }
    }
    internal partial class InvalidUrlRewriteFormatException : System.FormatException
    {
        public InvalidUrlRewriteFormatException(System.Xml.Linq.XElement element, string message) { }
        public InvalidUrlRewriteFormatException(System.Xml.Linq.XElement element, string message, System.Exception innerException) { }
        public int LineNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int LinePosition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal enum LogicalGrouping
    {
        MatchAll = 0,
        MatchAny = 1,
    }
    internal enum PatternSyntax
    {
        ECMAScript = 0,
        Wildcard = 1,
        ExactMatch = 2,
    }
    internal enum RedirectType
    {
        Permanent = 301,
        Found = 302,
        SeeOther = 303,
        Temporary = 307,
    }
    internal static partial class RewriteMapParser
    {
        public static Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMapCollection Parse(System.Xml.Linq.XElement xmlRoot) { throw null; }
    }
    internal static partial class RewriteTags
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
    internal static partial class ServerVariables
    {
        public static Microsoft.AspNetCore.Rewrite.PatternSegment FindServerVariable(string serverVariable, Microsoft.AspNetCore.Rewrite.ParserContext context, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UriMatchPart uriMatchPart, bool alwaysUseManagedServerVariables) { throw null; }
    }
    internal partial class UriMatchCondition : Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition
    {
        public UriMatchCondition(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.InputParser inputParser, string input, string pattern, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UriMatchPart uriMatchPart, bool ignoreCase, bool negate) { }
    }
    internal enum UriMatchPart
    {
        Full = 0,
        Path = 1,
    }
    internal partial class UrlRewriteFileParser
    {
        public UrlRewriteFileParser() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISUrlRewriteRule> Parse(System.IO.TextReader reader, bool alwaysUseManagedServerVariables) { throw null; }
    }
    internal partial class UrlRewriteRuleBuilder
    {
        public UrlRewriteRuleBuilder() { }
        public bool Enabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Global { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UriMatchPart UriMatchPart { get { throw null; } }
        public void AddUrlAction(Microsoft.AspNetCore.Rewrite.UrlAction action) { }
        public void AddUrlCondition(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition condition) { }
        public void AddUrlConditions(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Rewrite.IISUrlRewrite.Condition> conditions) { }
        public void AddUrlMatch(string input, bool ignoreCase = true, bool negate = false, Microsoft.AspNetCore.Rewrite.IISUrlRewrite.PatternSyntax patternSyntax = Microsoft.AspNetCore.Rewrite.IISUrlRewrite.PatternSyntax.ECMAScript) { }
        public Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISUrlRewriteRule Build() { throw null; }
        public void ConfigureConditionBehavior(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.LogicalGrouping logicalGrouping, bool trackAllCaptures) { }
    }
}

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal partial class ConditionMatchSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public ConditionMatchSegment(int index) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class DateTimeSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public DateTimeSegment(string segment) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReference) { throw null; }
    }
    internal partial class HeaderSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public HeaderSegment(string header) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class IsHttpsModSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public IsHttpsModSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class IsHttpsUrlSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public IsHttpsUrlSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class IsIPV6Segment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public IsIPV6Segment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class LiteralSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public LiteralSegment(string literal) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class LocalAddressSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public LocalAddressSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class LocalPortSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public LocalPortSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class QueryStringSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public QueryStringSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackRefernces, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RemoteAddressSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RemoteAddressSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RemotePortSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RemotePortSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RequestFileNameSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RequestFileNameSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RequestMethodSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RequestMethodSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RewriteMapSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RewriteMapSegment(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.IISRewriteMap rewriteMap, Microsoft.AspNetCore.Rewrite.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class RuleMatchSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public RuleMatchSegment(int index) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class SchemeSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public SchemeSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class ServerProtocolSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public ServerProtocolSegment() { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class ToLowerSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public ToLowerSegment(Microsoft.AspNetCore.Rewrite.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class UrlEncodeSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public UrlEncodeSegment(Microsoft.AspNetCore.Rewrite.Pattern pattern) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
    internal partial class UrlSegment : Microsoft.AspNetCore.Rewrite.PatternSegment
    {
        public UrlSegment() { }
        public UrlSegment(Microsoft.AspNetCore.Rewrite.IISUrlRewrite.UriMatchPart uriMatchPart) { }
        public override string Evaluate(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Rewrite.UrlActions
{
    internal partial class AbortAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public AbortAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
    internal partial class ChangeCookieAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public ChangeCookieAction(string name) { }
        internal ChangeCookieAction(string name, System.Func<System.DateTimeOffset> timeSource) { }
        public string Domain { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HttpOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan Lifetime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool Secure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
    internal partial class ForbiddenAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public ForbiddenAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
    internal partial class GoneAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public GoneAction() { }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
    internal partial class RedirectAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public RedirectAction(int statusCode, Microsoft.AspNetCore.Rewrite.Pattern pattern, bool queryStringAppend) { }
        public RedirectAction(int statusCode, Microsoft.AspNetCore.Rewrite.Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences) { }
        public bool EscapeBackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringAppend { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringDelete { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
    internal partial class RewriteAction : Microsoft.AspNetCore.Rewrite.UrlAction
    {
        public RewriteAction(Microsoft.AspNetCore.Rewrite.RuleResult result, Microsoft.AspNetCore.Rewrite.Pattern pattern, bool queryStringAppend) { }
        public RewriteAction(Microsoft.AspNetCore.Rewrite.RuleResult result, Microsoft.AspNetCore.Rewrite.Pattern pattern, bool queryStringAppend, bool queryStringDelete, bool escapeBackReferences) { }
        public bool EscapeBackReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringAppend { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool QueryStringDelete { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Rewrite.RuleResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void ApplyAction(Microsoft.AspNetCore.Rewrite.RewriteContext context, Microsoft.AspNetCore.Rewrite.BackReferenceCollection ruleBackReferences, Microsoft.AspNetCore.Rewrite.BackReferenceCollection conditionBackReferences) { }
    }
}

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal partial class ExactMatch : Microsoft.AspNetCore.Rewrite.UrlMatch
    {
        public ExactMatch(bool ignoreCase, string input, bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    internal partial class IntegerMatch : Microsoft.AspNetCore.Rewrite.UrlMatch
    {
        public IntegerMatch(int value, Microsoft.AspNetCore.Rewrite.UrlMatches.IntegerOperationType operation) { }
        public IntegerMatch(string value, Microsoft.AspNetCore.Rewrite.UrlMatches.IntegerOperationType operation) { }
        public override Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    internal enum IntegerOperationType
    {
        Equal = 0,
        Greater = 1,
        GreaterEqual = 2,
        Less = 3,
        LessEqual = 4,
        NotEqual = 5,
    }
    internal partial class RegexMatch : Microsoft.AspNetCore.Rewrite.UrlMatch
    {
        public RegexMatch(System.Text.RegularExpressions.Regex match, bool negate) { }
        public override Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(string pattern, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    internal partial class StringMatch : Microsoft.AspNetCore.Rewrite.UrlMatch
    {
        public StringMatch(string value, Microsoft.AspNetCore.Rewrite.UrlMatches.StringOperationType operation, bool ignoreCase) { }
        public override Microsoft.AspNetCore.Rewrite.MatchResults Evaluate(string input, Microsoft.AspNetCore.Rewrite.RewriteContext context) { throw null; }
    }
    internal enum StringOperationType
    {
        Equal = 0,
        Greater = 1,
        GreaterEqual = 2,
        Less = 3,
        LessEqual = 4,
    }
}
