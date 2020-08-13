// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct Candidate
    {
        public readonly Microsoft.AspNetCore.Http.Endpoint Endpoint;
        public readonly CandidateFlags Flags;
        public readonly System.Collections.Generic.KeyValuePair<string, object>[] Slots;
        public readonly (string parameterName, int segmentIndex, int slotIndex)[] Captures;
        public readonly (string parameterName, int segmentIndex, int slotIndex) CatchAll;
        public readonly (Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment pathSegment, int segmentIndex)[] ComplexSegments;
        public readonly System.Collections.Generic.KeyValuePair<string, IRouteConstraint>[] Constraints;
        public readonly int Score;
        public Candidate(Microsoft.AspNetCore.Http.Endpoint endpoint) { throw null; }
        public Candidate(Microsoft.AspNetCore.Http.Endpoint endpoint, int score, System.Collections.Generic.KeyValuePair<string, object>[] slots, System.ValueTuple<string, int, int>[] captures, in (string parameterName, int segmentIndex, int slotIndex) catchAll, System.ValueTuple<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment, int>[] complexSegments, System.Collections.Generic.KeyValuePair<string, Microsoft.AspNetCore.Routing.IRouteConstraint>[] constraints) { throw null; }
        [System.FlagsAttribute]
        public enum CandidateFlags
        {
            None = 0,
            HasDefaults = 1,
            HasCaptures = 2,
            HasCatchAll = 4,
            HasSlots = 7,
            HasComplexSegments = 8,
            HasConstraints = 16,
        }
    }

    internal partial class ILEmitTrieJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        internal System.Func<string, Microsoft.AspNetCore.Routing.Matching.PathSegment, int> _getDestination;
        public ILEmitTrieJumpTable(int defaultDestination, int exitDestination, System.ValueTuple<string, int>[] entries, bool? vectorize, Microsoft.AspNetCore.Routing.Matching.JumpTable fallback) { }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
        internal void InitializeILDelegate() { }
        internal System.Threading.Tasks.Task InitializeILDelegateAsync() { throw null; }
    }

    internal partial class LinearSearchJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        public LinearSearchJumpTable(int defaultDestination, int exitDestination, System.ValueTuple<string, int>[] entries) { }
        public override string DebuggerToString() { throw null; }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
    }

    internal partial class SingleEntryJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        public SingleEntryJumpTable(int defaultDestination, int exitDestination, string text, int destination) { }
        public override string DebuggerToString() { throw null; }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
    }

    internal partial class AmbiguousMatchException : System.Exception
    {
        protected AmbiguousMatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AmbiguousMatchException(string message) { }
    }

    public sealed partial class EndpointMetadataComparer : System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>
    {
        internal EndpointMetadataComparer(System.IServiceProvider services) { }
    }

    internal static partial class ILEmitTrieFactory
    {
        public const int NotAscii = -2147483648;
        public static System.Func<string, int, int, int> Create(int defaultDestination, int exitDestination, System.ValueTuple<string, int>[] entries, bool? vectorize) { throw null; }
        public static void EmitReturnDestination(System.Reflection.Emit.ILGenerator il, System.ValueTuple<string, int>[] entries) { }
        internal static bool ShouldVectorize(System.ValueTuple<string, int>[] entries) { throw null; }
    }

    internal partial class SingleEntryAsciiJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        public SingleEntryAsciiJumpTable(int defaultDestination, int exitDestination, string text, int destination) { }
        public override string DebuggerToString() { throw null; }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
    }

    internal partial class ZeroEntryJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        public ZeroEntryJumpTable(int defaultDestination, int exitDestination) { }
        public override string DebuggerToString() { throw null; }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
    }

    public sealed partial class HttpMethodMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointComparerPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy, Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy
    {
        internal static readonly string AccessControlRequestMethod;
        internal const string AnyMethod = "*";
        internal const string Http405EndpointDisplayName = "405 HTTP Method Not Supported";
        internal static readonly string OriginHeader;
        internal static readonly string PreflightHttpMethod;
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal readonly partial struct EdgeKey : System.IComparable, System.IComparable<Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy.EdgeKey>, System.IEquatable<Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy.EdgeKey>
        {
            public readonly bool IsCorsPreflightRequest;
            public readonly string HttpMethod;
            public EdgeKey(string httpMethod, bool isCorsPreflightRequest) { throw null; }
            public int CompareTo(Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy.EdgeKey other) { throw null; }
            public int CompareTo(object obj) { throw null; }
            public bool Equals(Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy.EdgeKey other) { throw null; }
            public override bool Equals(object obj) { throw null; }
            public override int GetHashCode() { throw null; }
            public override string ToString() { throw null; }
        }
    }

    internal static partial class Ascii
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool AsciiIgnoreCaseEquals(char charA, char charB) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool AsciiIgnoreCaseEquals(System.ReadOnlySpan<char> a, System.ReadOnlySpan<char> b, int length) { throw null; }
        public static bool IsAscii(string text) { throw null; }
    }

    public sealed partial class CandidateSet
    {
        internal Microsoft.AspNetCore.Routing.Matching.CandidateState[] Candidates;
        internal CandidateSet(Microsoft.AspNetCore.Routing.Matching.CandidateState[] candidates) { }
        internal CandidateSet(Microsoft.AspNetCore.Routing.Matching.Candidate[] candidates) { }
        internal static bool IsValidCandidate(ref Microsoft.AspNetCore.Routing.Matching.CandidateState candidate) { throw null; }
        internal static void SetValidity(ref Microsoft.AspNetCore.Routing.Matching.CandidateState candidate, bool value) { }
    }

    public partial struct CandidateState
    {
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
    }

    internal sealed partial class DataSourceDependentMatcher : Microsoft.AspNetCore.Routing.Matching.Matcher
    {
        public DataSourceDependentMatcher(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, Microsoft.AspNetCore.Routing.Matching.DataSourceDependentMatcher.Lifetime lifetime, System.Func<Microsoft.AspNetCore.Routing.Matching.MatcherBuilder> matcherBuilderFactory) { }
        internal Microsoft.AspNetCore.Routing.Matching.Matcher CurrentMatcher { get { throw null; } }
        public override System.Threading.Tasks.Task MatchAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public sealed partial class Lifetime : System.IDisposable
        {
            public Lifetime() { }
            public Microsoft.AspNetCore.Routing.DataSourceDependentCache<Microsoft.AspNetCore.Routing.Matching.Matcher> Cache { get { throw null; } set { } }
            public void Dispose() { }
        }
    }

    internal sealed partial class DefaultEndpointSelector : Microsoft.AspNetCore.Routing.Matching.EndpointSelector
    {
        public DefaultEndpointSelector() { }
        internal static void Select(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateState[] candidateState) { }
        public override System.Threading.Tasks.Task SelectAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidateSet) { throw null; }
    }

    internal sealed partial class DfaMatcher : Microsoft.AspNetCore.Routing.Matching.Matcher
    {
        public DfaMatcher(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Routing.Matching.DfaMatcher> logger, Microsoft.AspNetCore.Routing.Matching.EndpointSelector selector, Microsoft.AspNetCore.Routing.Matching.DfaState[] states, int maxSegmentCount) { }
        internal (Microsoft.AspNetCore.Routing.Matching.Candidate[] candidates, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy[] policies) FindCandidateSet(Microsoft.AspNetCore.Http.HttpContext httpContext, string path, System.ReadOnlySpan<Microsoft.AspNetCore.Routing.Matching.PathSegment> segments) { throw null; }
        public sealed override System.Threading.Tasks.Task MatchAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        internal static partial class EventIds
        {
            public static readonly Microsoft.Extensions.Logging.EventId CandidateNotValid;
            public static readonly Microsoft.Extensions.Logging.EventId CandidateRejectedByComplexSegment;
            public static readonly Microsoft.Extensions.Logging.EventId CandidateRejectedByConstraint;
            public static readonly Microsoft.Extensions.Logging.EventId CandidatesFound;
            public static readonly Microsoft.Extensions.Logging.EventId CandidatesNotFound;
            public static readonly Microsoft.Extensions.Logging.EventId CandidateValid;
        }
    }

    internal partial class DictionaryJumpTable : Microsoft.AspNetCore.Routing.Matching.JumpTable
    {
        public DictionaryJumpTable(int defaultDestination, int exitDestination, System.ValueTuple<string, int>[] entries) { }
        public override string DebuggerToString() { throw null; }
        public override int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment) { throw null; }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    internal partial class DfaNode
    {
        public DfaNode() { }
        public Microsoft.AspNetCore.Routing.Matching.DfaNode CatchAll { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Label { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.Dictionary<string, Microsoft.AspNetCore.Routing.Matching.DfaNode> Literals { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Http.Endpoint> Matches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy NodeBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Routing.Matching.DfaNode Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PathDepth { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.Dictionary<object, Microsoft.AspNetCore.Routing.Matching.DfaNode> PolicyEdges { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void AddLiteral(string literal, Microsoft.AspNetCore.Routing.Matching.DfaNode node) { }
        public void AddMatch(Microsoft.AspNetCore.Http.Endpoint endpoint) { }
        public void AddMatches(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints) { }
        public void AddPolicyEdge(object state, Microsoft.AspNetCore.Routing.Matching.DfaNode node) { }
        public void Visit(System.Action<Microsoft.AspNetCore.Routing.Matching.DfaNode> visitor) { }
    }

    internal static partial class FastPathTokenizer
    {
        public static int Tokenize(string path, System.Span<Microsoft.AspNetCore.Routing.Matching.PathSegment> segments) { throw null; }
    }

    internal partial class DfaMatcherBuilder : Microsoft.AspNetCore.Routing.Matching.MatcherBuilder
    {
        public DfaMatcherBuilder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Routing.ParameterPolicyFactory parameterPolicyFactory, Microsoft.AspNetCore.Routing.Matching.EndpointSelector selector, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.MatcherPolicy> policies) { }
        internal EndpointComparer Comparer { get; }
        internal bool UseCorrectCatchAllBehavior { get; set; }
        public override void AddEndpoint(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) { }
        public override Microsoft.AspNetCore.Routing.Matching.Matcher Build() { throw null; }
        public Microsoft.AspNetCore.Routing.Matching.DfaNode BuildDfaTree(bool includeLabel = false) { throw null; }
        internal Microsoft.AspNetCore.Routing.Matching.Candidate CreateCandidate(Microsoft.AspNetCore.Http.Endpoint endpoint, int score) { throw null; }
        internal Microsoft.AspNetCore.Routing.Matching.Candidate[] CreateCandidates(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct DfaState
    {
        public readonly Candidate[] Candidates;
        public readonly IEndpointSelectorPolicy[] Policies;
        public readonly JumpTable PathTransitions;
        public readonly PolicyJumpTable PolicyTransitions;
        public DfaState(Microsoft.AspNetCore.Routing.Matching.Candidate[] candidates, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy[] policies, Microsoft.AspNetCore.Routing.Matching.JumpTable pathTransitions, Microsoft.AspNetCore.Routing.Matching.PolicyJumpTable policyTransitions) { throw null; }
        public string DebuggerToString() { throw null; }
    }

    internal partial class EndpointComparer : System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>, System.Collections.Generic.IEqualityComparer<Microsoft.AspNetCore.Http.Endpoint>
    {
        public EndpointComparer(Microsoft.AspNetCore.Routing.Matching.IEndpointComparerPolicy[] policies) { }
        public int Compare(Microsoft.AspNetCore.Http.Endpoint x, Microsoft.AspNetCore.Http.Endpoint y) { throw null; }
        public bool Equals(Microsoft.AspNetCore.Http.Endpoint x, Microsoft.AspNetCore.Http.Endpoint y) { throw null; }
        public int GetHashCode(Microsoft.AspNetCore.Http.Endpoint obj) { throw null; }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    internal abstract partial class JumpTable
    {
        protected JumpTable() { }
        public virtual string DebuggerToString() { throw null; }
        public abstract int GetDestination(string path, Microsoft.AspNetCore.Routing.Matching.PathSegment segment);
    }

    internal abstract partial class Matcher
    {
        protected Matcher() { }
        public abstract System.Threading.Tasks.Task MatchAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    internal abstract partial class MatcherFactory
    {
        protected MatcherFactory() { }
        public abstract Microsoft.AspNetCore.Routing.Matching.Matcher CreateMatcher(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct PathSegment : System.IEquatable<Microsoft.AspNetCore.Routing.Matching.PathSegment>
    {
        public readonly int Start;
        public readonly int Length;
        public PathSegment(int start, int length) { throw null; }
        public bool Equals(Microsoft.AspNetCore.Routing.Matching.PathSegment other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }

    internal abstract partial class MatcherBuilder
    {
        protected MatcherBuilder() { }
        public abstract void AddEndpoint(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint);
        public abstract Microsoft.AspNetCore.Routing.Matching.Matcher Build();
    }
}

namespace Microsoft.AspNetCore.Routing
{

    internal partial class RoutingMarkerService
    {
        public RoutingMarkerService() { }
    }

    internal partial class UriBuilderContextPooledObjectPolicy : Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<Microsoft.AspNetCore.Routing.UriBuildingContext>
    {
        public UriBuilderContextPooledObjectPolicy() { }
        public Microsoft.AspNetCore.Routing.UriBuildingContext Create() { throw null; }
        public bool Return(Microsoft.AspNetCore.Routing.UriBuildingContext obj) { throw null; }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct PathTokenizer : System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.Generic.IReadOnlyCollection<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.IEnumerable
    {
        private readonly string _path;
        private int _count;
        public PathTokenizer(Microsoft.AspNetCore.Http.PathString path) { throw null; }
        public int Count { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringSegment this[int index] { get { throw null; } }
        public Microsoft.AspNetCore.Routing.PathTokenizer.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<Microsoft.Extensions.Primitives.StringSegment> System.Collections.Generic.IEnumerable<Microsoft.Extensions.Primitives.StringSegment>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<Microsoft.Extensions.Primitives.StringSegment>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly string _path;
            private int _index;
            private int _length;
            public Enumerator(Microsoft.AspNetCore.Routing.PathTokenizer tokenizer) { throw null; }
            public Microsoft.Extensions.Primitives.StringSegment Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }

    public partial class RouteOptions
    {
        internal System.Collections.Generic.ICollection<Microsoft.AspNetCore.Routing.EndpointDataSource> EndpointDataSources { get { throw null; } set { } }
    }

    internal sealed partial class EndpointMiddleware
    {
        internal const string AuthorizationMiddlewareInvokedKey = "__AuthorizationMiddlewareWithEndpointInvoked";
        internal const string CorsMiddlewareInvokedKey = "__CorsMiddlewareWithEndpointInvoked";
        public EndpointMiddleware(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Routing.EndpointMiddleware> logger, Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Routing.RouteOptions> routeOptions) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }

    internal sealed partial class DataSourceDependentCache<T> : System.IDisposable where T : class
    {
        public DataSourceDependentCache(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint>, T> initialize) { }
        public T Value { get { throw null; } }
        public void Dispose() { }
        public T EnsureInitialized() { throw null; }
    }

    internal partial class DefaultEndpointConventionBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder
    {
        public DefaultEndpointConventionBuilder(Microsoft.AspNetCore.Builder.EndpointBuilder endpointBuilder) { }
        internal Microsoft.AspNetCore.Builder.EndpointBuilder EndpointBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Add(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> convention) { }
        public Microsoft.AspNetCore.Http.Endpoint Build() { throw null; }
    }

    internal partial class DefaultEndpointRouteBuilder : Microsoft.AspNetCore.Routing.IEndpointRouteBuilder
    {
        public DefaultEndpointRouteBuilder(Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder) { }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder ApplicationBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.ICollection<Microsoft.AspNetCore.Routing.EndpointDataSource> DataSources { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IServiceProvider ServiceProvider { get { throw null; } }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder CreateApplicationBuilder() { throw null; }
    }

    internal sealed partial class DefaultLinkGenerator : Microsoft.AspNetCore.Routing.LinkGenerator, System.IDisposable
    {
        public DefaultLinkGenerator(Microsoft.AspNetCore.Routing.ParameterPolicyFactory parameterPolicyFactory, Microsoft.AspNetCore.Routing.Template.TemplateBinderFactory binderFactory, Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Routing.RouteOptions> routeOptions, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Routing.DefaultLinkGenerator> logger, System.IServiceProvider serviceProvider) { }
        public void Dispose() { }
        public static Microsoft.AspNetCore.Routing.RouteValueDictionary GetAmbientValues(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public override string GetPathByAddress<TAddress>(Microsoft.AspNetCore.Http.HttpContext httpContext, TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues = null, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public override string GetPathByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        internal Microsoft.AspNetCore.Routing.Template.TemplateBinder GetTemplateBinder(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) { throw null; }
        public override string GetUriByAddress<TAddress>(Microsoft.AspNetCore.Http.HttpContext httpContext, TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues = null, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public override string GetUriByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public string GetUriByEndpoints(System.Collections.Generic.List<Microsoft.AspNetCore.Routing.RouteEndpoint> endpoints, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase, Microsoft.AspNetCore.Http.FragmentString fragment, Microsoft.AspNetCore.Routing.LinkOptions options) { throw null; }
        internal bool TryProcessTemplate(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteEndpoint endpoint, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues, Microsoft.AspNetCore.Routing.LinkOptions options, out (Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Http.QueryString query) result) { throw null; }
    }

    internal partial class DefaultLinkParser : Microsoft.AspNetCore.Routing.LinkParser, System.IDisposable
    {
        public DefaultLinkParser(Microsoft.AspNetCore.Routing.ParameterPolicyFactory parameterPolicyFactory, Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Routing.DefaultLinkParser> logger, System.IServiceProvider serviceProvider) { }
        public void Dispose() { }
        internal Microsoft.AspNetCore.Routing.DefaultLinkParser.MatcherState GetMatcherState(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) { throw null; }
        public override Microsoft.AspNetCore.Routing.RouteValueDictionary ParsePathByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Http.PathString path) { throw null; }
        internal bool TryParse(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint, Microsoft.AspNetCore.Http.PathString path, out Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal readonly partial struct MatcherState
        {
            private readonly object _dummy;
            public readonly Microsoft.AspNetCore.Routing.RoutePatternMatcher Matcher;
            public readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<IRouteConstraint>> Constraints;
            public MatcherState(Microsoft.AspNetCore.Routing.RoutePatternMatcher matcher, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Microsoft.AspNetCore.Routing.IRouteConstraint>> constraints) { throw null; }
            public void Deconstruct(out Microsoft.AspNetCore.Routing.RoutePatternMatcher matcher, out System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Microsoft.AspNetCore.Routing.IRouteConstraint>> constraints) { throw null; }
        }
    }

    internal partial class DefaultParameterPolicyFactory : Microsoft.AspNetCore.Routing.ParameterPolicyFactory
    {
        public DefaultParameterPolicyFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Routing.RouteOptions> options, System.IServiceProvider serviceProvider) { }
        public override Microsoft.AspNetCore.Routing.IParameterPolicy Create(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter, Microsoft.AspNetCore.Routing.IParameterPolicy parameterPolicy) { throw null; }
        public override Microsoft.AspNetCore.Routing.IParameterPolicy Create(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter, string inlineText) { throw null; }
    }

    internal sealed partial class EndpointNameAddressScheme : Microsoft.AspNetCore.Routing.IEndpointAddressScheme<string>, System.IDisposable
    {
        public EndpointNameAddressScheme(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        internal System.Collections.Generic.Dictionary<string, Microsoft.AspNetCore.Http.Endpoint[]> Entries { get { throw null; } }
        public void Dispose() { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> FindEndpoints(string address) { throw null; }
    }

    internal sealed partial class EndpointRoutingMiddleware
    {
        public EndpointRoutingMiddleware(Microsoft.AspNetCore.Routing.Matching.MatcherFactory matcherFactory, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Routing.EndpointRoutingMiddleware> logger, Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpointRouteBuilder, System.Diagnostics.DiagnosticListener diagnosticListener, Microsoft.AspNetCore.Http.RequestDelegate next) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }

    internal partial class ModelEndpointDataSource : Microsoft.AspNetCore.Routing.EndpointDataSource
    {
        public ModelEndpointDataSource() { }
        internal System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Builder.EndpointBuilder> EndpointBuilders { get { throw null; } }
        public override System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get { throw null; } }
        public Microsoft.AspNetCore.Builder.IEndpointConventionBuilder AddEndpointBuilder(Microsoft.AspNetCore.Builder.EndpointBuilder endpointBuilder) { throw null; }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }

    internal partial class NullRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        public static readonly Microsoft.AspNetCore.Routing.NullRouter Instance;
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }

    internal partial class RoutePatternMatcher
    {
        public RoutePatternMatcher(Microsoft.AspNetCore.Routing.Patterns.RoutePattern pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults) { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePattern RoutePattern { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal static bool MatchComplexSegment(Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment routeSegment, System.ReadOnlySpan<char> requestSegment, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
        public bool TryMatch(Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }

    internal sealed partial class RouteValuesAddressScheme : Microsoft.AspNetCore.Routing.IEndpointAddressScheme<Microsoft.AspNetCore.Routing.RouteValuesAddress>, System.IDisposable
    {
        public RouteValuesAddressScheme(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource) { }
        internal Microsoft.AspNetCore.Routing.RouteValuesAddressScheme.StateEntry State { get { throw null; } }
        public void Dispose() { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> FindEndpoints(Microsoft.AspNetCore.Routing.RouteValuesAddress address) { throw null; }
        internal partial class StateEntry
        {
            public readonly System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Tree.OutboundMatch> AllMatches;
            public readonly Microsoft.AspNetCore.Routing.Tree.LinkGenerationDecisionTree AllMatchesLinkGenerationTree;
            public readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Tree.OutboundMatchResult>> NamedMatches;
            public StateEntry(System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Tree.OutboundMatch> allMatches, Microsoft.AspNetCore.Routing.Tree.LinkGenerationDecisionTree allMatchesLinkGenerationTree, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Tree.OutboundMatchResult>> namedMatches) { }
        }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    internal partial class UriBuildingContext
    {
        public UriBuildingContext(System.Text.Encodings.Web.UrlEncoder urlEncoder) { }
        public bool AppendTrailingSlash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool LowercaseQueryStrings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool LowercaseUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.TextWriter PathWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IO.TextWriter QueryWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Accept(string value) { throw null; }
        public bool Accept(string value, bool encodeSlashes) { throw null; }
        public bool Buffer(string value) { throw null; }
        public void Clear() { }
        internal void EncodeValue(string value, int start, int characterCount, bool encodeSlashes) { }
        public void EndSegment() { }
        public void Remove(string literal) { }
        public Microsoft.AspNetCore.Http.PathString ToPathString() { throw null; }
        public Microsoft.AspNetCore.Http.QueryString ToQueryString() { throw null; }
        public override string ToString() { throw null; }
    }
}

namespace Microsoft.AspNetCore.Routing.DecisionTree
{

    internal partial class DecisionCriterion<TItem>
    {
        public DecisionCriterion() { }
        public System.Collections.Generic.Dictionary<object, Microsoft.AspNetCore.Routing.DecisionTree.DecisionTreeNode<TItem>> Branches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }

    internal static partial class DecisionTreeBuilder<TItem>
    {
        public static Microsoft.AspNetCore.Routing.DecisionTree.DecisionTreeNode<TItem> GenerateTree(System.Collections.Generic.IReadOnlyList<TItem> items, Microsoft.AspNetCore.Routing.DecisionTree.IClassifier<TItem> classifier) { throw null; }
    }

    internal partial class DecisionTreeNode<TItem>
    {
        public DecisionTreeNode() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.DecisionTree.DecisionCriterion<TItem>> Criteria { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<TItem> Matches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct DecisionCriterionValue
    {
        private readonly object _value;
        public DecisionCriterionValue(object value) { throw null; }
        public object Value { get { throw null; } }
    }

    internal partial interface IClassifier<TItem>
    {
        System.Collections.Generic.IEqualityComparer<object> ValueComparer { get; }
        System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.DecisionTree.DecisionCriterionValue> GetCriteria(TItem item);
    }
}

namespace Microsoft.AspNetCore.Routing.Tree
{

    public partial class TreeRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        internal TreeRouter(Microsoft.AspNetCore.Routing.Tree.UrlMatchingTree[] trees, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Tree.OutboundRouteEntry> linkGenerationEntries, System.Text.Encodings.Web.UrlEncoder urlEncoder, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Routing.UriBuildingContext> objectPool, Microsoft.Extensions.Logging.ILogger routeLogger, Microsoft.Extensions.Logging.ILogger constraintLogger, int version) { }
        internal System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Tree.UrlMatchingTree> MatchingTrees { get { throw null; } }
    }

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerDisplayString,nq}")]
    internal partial class LinkGenerationDecisionTree
    {
        public LinkGenerationDecisionTree(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Tree.OutboundMatch> entries) { }
        internal string DebuggerDisplayString { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.Tree.OutboundMatchResult> GetMatches(Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues) { throw null; }
    }

    public partial class TreeRouteBuilder
    {
        internal TreeRouteBuilder(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Routing.UriBuildingContext> objectPool, Microsoft.AspNetCore.Routing.IInlineConstraintResolver constraintResolver) { }
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct OutboundMatchResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public OutboundMatchResult(Microsoft.AspNetCore.Routing.Tree.OutboundMatch match, bool isFallbackMatch) { throw null; }
        public bool IsFallbackMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Routing.Tree.OutboundMatch Match { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}

namespace Microsoft.AspNetCore.Routing.Patterns
{

    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternPathSegment
    {
        internal RoutePatternPathSegment(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart> parts) { }
        internal string DebuggerToString() { throw null; }
        internal static string DebuggerToString(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart> parts) { throw null; }
    }

    internal static partial class RouteParameterParser
    {
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParseRouteParameter(string parameter) { throw null; }
    }

    internal partial class DefaultRoutePatternTransformer : Microsoft.AspNetCore.Routing.Patterns.RoutePatternTransformer
    {
        public DefaultRoutePatternTransformer(Microsoft.AspNetCore.Routing.ParameterPolicyFactory policyFactory) { }
        public override Microsoft.AspNetCore.Routing.Patterns.RoutePattern SubstituteRequiredValues(Microsoft.AspNetCore.Routing.Patterns.RoutePattern original, object requiredValues) { throw null; }
    }

    internal static partial class RoutePatternParser
    {
        internal static readonly char[] InvalidParameterNameChars;
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Parse(string pattern) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Routing.Template
{
    public partial class TemplateBinder
    {
        internal TemplateBinder(System.Text.Encodings.Web.UrlEncoder urlEncoder, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Routing.UriBuildingContext> pool, Microsoft.AspNetCore.Routing.Patterns.RoutePattern pattern, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IEnumerable<string> requiredKeys, System.Collections.Generic.IEnumerable<System.ValueTuple<string, Microsoft.AspNetCore.Routing.IParameterPolicy>> parameterPolicies) { }
        internal TemplateBinder(System.Text.Encodings.Web.UrlEncoder urlEncoder, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Routing.UriBuildingContext> pool, Microsoft.AspNetCore.Routing.Patterns.RoutePattern pattern, System.Collections.Generic.IEnumerable<System.ValueTuple<string, Microsoft.AspNetCore.Routing.IParameterPolicy>> parameterPolicies) { }
        internal TemplateBinder(System.Text.Encodings.Web.UrlEncoder urlEncoder, Microsoft.Extensions.ObjectPool.ObjectPool<Microsoft.AspNetCore.Routing.UriBuildingContext> pool, Microsoft.AspNetCore.Routing.Template.RouteTemplate template, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults) { }
        internal bool TryBindValues(Microsoft.AspNetCore.Routing.RouteValueDictionary acceptedValues, Microsoft.AspNetCore.Routing.LinkOptions options, Microsoft.AspNetCore.Routing.LinkOptions globalOptions, out (Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Http.QueryString query) result) { throw null; }
    }

    public static partial class RoutePrecedence
    {
        internal static decimal ComputeInbound(Microsoft.AspNetCore.Routing.Patterns.RoutePattern routePattern) { throw null; }
        internal static decimal ComputeOutbound(Microsoft.AspNetCore.Routing.Patterns.RoutePattern routePattern) { throw null; }
    }
}
