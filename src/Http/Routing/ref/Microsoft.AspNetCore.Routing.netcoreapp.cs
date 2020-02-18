// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class EndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, Microsoft.AspNetCore.Routing.Patterns.RoutePattern pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapDelete(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapMethods(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, System.Collections.Generic.IEnumerable<string> httpMethods, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPost(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPut(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
    }
    public static partial class EndpointRoutingApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseEndpoints(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, System.Action<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder> configure) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRouting(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder) { throw null; }
    }
    public static partial class FallbackEndpointRouteBuilderExtensions
    {
        public static readonly string DefaultPattern;
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallback(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallback(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { throw null; }
    }
    public static partial class MapRouteRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string template) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string template, object defaults) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, object dataTokens) { throw null; }
    }
    public partial class RouterMiddleware
    {
        public RouterMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Routing.IRouter router) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public static partial class RoutingBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRouter(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, Microsoft.AspNetCore.Routing.IRouter router) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRouter(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder, System.Action<Microsoft.AspNetCore.Routing.IRouteBuilder> action) { throw null; }
    }
    public static partial class RoutingEndpointConventionBuilderExtensions
    {
        public static TBuilder RequireHost<TBuilder>(this TBuilder builder, params string[] hosts) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, System.Func<Microsoft.AspNetCore.Builder.EndpointBuilder, string> func) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, string displayName) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
        public static TBuilder WithMetadata<TBuilder>(this TBuilder builder, params object[] items) where TBuilder : Microsoft.AspNetCore.Builder.IEndpointConventionBuilder { throw null; }
    }
}
namespace Microsoft.AspNetCore.Routing
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerDisplayString,nq}")]
    public sealed partial class CompositeEndpointDataSource : Microsoft.AspNetCore.Routing.EndpointDataSource
    {
        public CompositeEndpointDataSource(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> endpointDataSources) { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> DataSources { get { throw null; } }
        public override System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get { throw null; } }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }
    public sealed partial class DataTokensMetadata : Microsoft.AspNetCore.Routing.IDataTokensMetadata
    {
        public DataTokensMetadata(System.Collections.Generic.IReadOnlyDictionary<string, object> dataTokens) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> DataTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class DefaultEndpointDataSource : Microsoft.AspNetCore.Routing.EndpointDataSource
    {
        public DefaultEndpointDataSource(params Microsoft.AspNetCore.Http.Endpoint[] endpoints) { }
        public DefaultEndpointDataSource(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints) { }
        public override System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get { throw null; } }
        public override Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }
    public partial class DefaultInlineConstraintResolver : Microsoft.AspNetCore.Routing.IInlineConstraintResolver
    {
        public DefaultInlineConstraintResolver(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Routing.RouteOptions> routeOptions, System.IServiceProvider serviceProvider) { }
        public virtual Microsoft.AspNetCore.Routing.IRouteConstraint ResolveConstraint(string inlineConstraint) { throw null; }
    }
    public abstract partial class EndpointDataSource
    {
        protected EndpointDataSource() { }
        public abstract System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { get; }
        public abstract Microsoft.Extensions.Primitives.IChangeToken GetChangeToken();
    }
    public partial class EndpointNameMetadata : Microsoft.AspNetCore.Routing.IEndpointNameMetadata
    {
        public EndpointNameMetadata(string endpointName) { }
        public string EndpointName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public sealed partial class HostAttribute : System.Attribute, Microsoft.AspNetCore.Routing.IHostMetadata
    {
        public HostAttribute(string host) { }
        public HostAttribute(params string[] hosts) { }
        public System.Collections.Generic.IReadOnlyList<string> Hosts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public sealed partial class HttpMethodMetadata : Microsoft.AspNetCore.Routing.IHttpMethodMetadata
    {
        public HttpMethodMetadata(System.Collections.Generic.IEnumerable<string> httpMethods) { }
        public HttpMethodMetadata(System.Collections.Generic.IEnumerable<string> httpMethods, bool acceptCorsPreflight) { }
        public bool AcceptCorsPreflight { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<string> HttpMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface IDataTokensMetadata
    {
        System.Collections.Generic.IReadOnlyDictionary<string, object> DataTokens { get; }
    }
    public partial interface IDynamicEndpointMetadata
    {
        bool IsDynamic { get; }
    }
    public partial interface IEndpointAddressScheme<TAddress>
    {
        System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Endpoint> FindEndpoints(TAddress address);
    }
    public partial interface IEndpointNameMetadata
    {
        string EndpointName { get; }
    }
    public partial interface IEndpointRouteBuilder
    {
        System.Collections.Generic.ICollection<Microsoft.AspNetCore.Routing.EndpointDataSource> DataSources { get; }
        System.IServiceProvider ServiceProvider { get; }
        Microsoft.AspNetCore.Builder.IApplicationBuilder CreateApplicationBuilder();
    }
    public partial interface IHostMetadata
    {
        System.Collections.Generic.IReadOnlyList<string> Hosts { get; }
    }
    public partial interface IHttpMethodMetadata
    {
        bool AcceptCorsPreflight { get; }
        System.Collections.Generic.IReadOnlyList<string> HttpMethods { get; }
    }
    public partial interface IInlineConstraintResolver
    {
        Microsoft.AspNetCore.Routing.IRouteConstraint ResolveConstraint(string inlineConstraint);
    }
    public partial interface INamedRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        string Name { get; }
    }
    public static partial class InlineRouteParameterParser
    {
        public static Microsoft.AspNetCore.Routing.Template.TemplatePart ParseRouteParameter(string routeParameter) { throw null; }
    }
    public partial interface IRouteBuilder
    {
        Microsoft.AspNetCore.Builder.IApplicationBuilder ApplicationBuilder { get; }
        Microsoft.AspNetCore.Routing.IRouter DefaultHandler { get; set; }
        System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.IRouter> Routes { get; }
        System.IServiceProvider ServiceProvider { get; }
        Microsoft.AspNetCore.Routing.IRouter Build();
    }
    public partial interface IRouteCollection : Microsoft.AspNetCore.Routing.IRouter
    {
        void Add(Microsoft.AspNetCore.Routing.IRouter router);
    }
    public partial interface IRouteNameMetadata
    {
        string RouteName { get; }
    }
    public partial interface ISuppressLinkGenerationMetadata
    {
        bool SuppressLinkGeneration { get; }
    }
    public partial interface ISuppressMatchingMetadata
    {
        bool SuppressMatching { get; }
    }
    public static partial class LinkGeneratorEndpointNameAddressExtensions
    {
        public static string GetPathByName(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string endpointName, object values, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetPathByName(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string endpointName, object values, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByName(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string endpointName, object values, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByName(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string endpointName, object values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
    }
    public static partial class LinkGeneratorRouteValuesAddressExtensions
    {
        public static string GetPathByRouteValues(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string routeName, object values, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetPathByRouteValues(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string routeName, object values, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByRouteValues(this Microsoft.AspNetCore.Routing.LinkGenerator generator, Microsoft.AspNetCore.Http.HttpContext httpContext, string routeName, object values, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
        public static string GetUriByRouteValues(this Microsoft.AspNetCore.Routing.LinkGenerator generator, string routeName, object values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null) { throw null; }
    }
    public abstract partial class LinkParser
    {
        protected LinkParser() { }
        public abstract Microsoft.AspNetCore.Routing.RouteValueDictionary ParsePathByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Http.PathString path);
    }
    public static partial class LinkParserEndpointNameAddressExtensions
    {
        public static Microsoft.AspNetCore.Routing.RouteValueDictionary ParsePathByEndpointName(this Microsoft.AspNetCore.Routing.LinkParser parser, string endpointName, Microsoft.AspNetCore.Http.PathString path) { throw null; }
    }
    public abstract partial class MatcherPolicy
    {
        protected MatcherPolicy() { }
        public abstract int Order { get; }
        protected static bool ContainsDynamicEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
    }
    public abstract partial class ParameterPolicyFactory
    {
        protected ParameterPolicyFactory() { }
        public abstract Microsoft.AspNetCore.Routing.IParameterPolicy Create(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter, Microsoft.AspNetCore.Routing.IParameterPolicy parameterPolicy);
        public Microsoft.AspNetCore.Routing.IParameterPolicy Create(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter, Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference reference) { throw null; }
        public abstract Microsoft.AspNetCore.Routing.IParameterPolicy Create(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart parameter, string inlineText);
    }
    public static partial class RequestDelegateRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapDelete(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapDelete(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, Microsoft.AspNetCore.Routing.RouteData, System.Threading.Tasks.Task> handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapGet(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapGet(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, Microsoft.AspNetCore.Routing.RouteData, System.Threading.Tasks.Task> handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewareDelete(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewareGet(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewarePost(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewarePut(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewareRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapMiddlewareVerb(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string verb, string template, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> action) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapPost(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapPost(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, Microsoft.AspNetCore.Routing.RouteData, System.Threading.Tasks.Task> handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapPut(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapPut(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, Microsoft.AspNetCore.Routing.RouteData, System.Threading.Tasks.Task> handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapVerb(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string verb, string template, Microsoft.AspNetCore.Http.RequestDelegate handler) { throw null; }
        public static Microsoft.AspNetCore.Routing.IRouteBuilder MapVerb(this Microsoft.AspNetCore.Routing.IRouteBuilder builder, string verb, string template, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, Microsoft.AspNetCore.Routing.RouteData, System.Threading.Tasks.Task> handler) { throw null; }
    }
    public partial class Route : Microsoft.AspNetCore.Routing.RouteBase
    {
        public Route(Microsoft.AspNetCore.Routing.IRouter target, string routeTemplate, Microsoft.AspNetCore.Routing.IInlineConstraintResolver inlineConstraintResolver) : base (default(string), default(string), default(Microsoft.AspNetCore.Routing.IInlineConstraintResolver), default(Microsoft.AspNetCore.Routing.RouteValueDictionary), default(System.Collections.Generic.IDictionary<string, object>), default(Microsoft.AspNetCore.Routing.RouteValueDictionary)) { }
        public Route(Microsoft.AspNetCore.Routing.IRouter target, string routeTemplate, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, Microsoft.AspNetCore.Routing.IInlineConstraintResolver inlineConstraintResolver) : base (default(string), default(string), default(Microsoft.AspNetCore.Routing.IInlineConstraintResolver), default(Microsoft.AspNetCore.Routing.RouteValueDictionary), default(System.Collections.Generic.IDictionary<string, object>), default(Microsoft.AspNetCore.Routing.RouteValueDictionary)) { }
        public Route(Microsoft.AspNetCore.Routing.IRouter target, string routeName, string routeTemplate, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, Microsoft.AspNetCore.Routing.IInlineConstraintResolver inlineConstraintResolver) : base (default(string), default(string), default(Microsoft.AspNetCore.Routing.IInlineConstraintResolver), default(Microsoft.AspNetCore.Routing.RouteValueDictionary), default(System.Collections.Generic.IDictionary<string, object>), default(Microsoft.AspNetCore.Routing.RouteValueDictionary)) { }
        public string RouteTemplate { get { throw null; } }
        protected override System.Threading.Tasks.Task OnRouteMatched(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
        protected override Microsoft.AspNetCore.Routing.VirtualPathData OnVirtualPathGenerated(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
    }
    public abstract partial class RouteBase : Microsoft.AspNetCore.Routing.INamedRouter, Microsoft.AspNetCore.Routing.IRouter
    {
        public RouteBase(string template, string name, Microsoft.AspNetCore.Routing.IInlineConstraintResolver constraintResolver, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults, System.Collections.Generic.IDictionary<string, object> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens) { }
        protected virtual Microsoft.AspNetCore.Routing.IInlineConstraintResolver ConstraintResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> Constraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public virtual Microsoft.AspNetCore.Routing.RouteValueDictionary DataTokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public virtual Microsoft.AspNetCore.Routing.RouteValueDictionary Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public virtual string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public virtual Microsoft.AspNetCore.Routing.Template.RouteTemplate ParsedTemplate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        protected static System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> GetConstraints(Microsoft.AspNetCore.Routing.IInlineConstraintResolver inlineConstraintResolver, Microsoft.AspNetCore.Routing.Template.RouteTemplate parsedTemplate, System.Collections.Generic.IDictionary<string, object> constraints) { throw null; }
        protected static Microsoft.AspNetCore.Routing.RouteValueDictionary GetDefaults(Microsoft.AspNetCore.Routing.Template.RouteTemplate parsedTemplate, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults) { throw null; }
        public virtual Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        protected abstract System.Threading.Tasks.Task OnRouteMatched(Microsoft.AspNetCore.Routing.RouteContext context);
        protected abstract Microsoft.AspNetCore.Routing.VirtualPathData OnVirtualPathGenerated(Microsoft.AspNetCore.Routing.VirtualPathContext context);
        public virtual System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class RouteBuilder : Microsoft.AspNetCore.Routing.IRouteBuilder
    {
        public RouteBuilder(Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder) { }
        public RouteBuilder(Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder, Microsoft.AspNetCore.Routing.IRouter defaultHandler) { }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder ApplicationBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.IRouter DefaultHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.IRouter> Routes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.IServiceProvider ServiceProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.IRouter Build() { throw null; }
    }
    public partial class RouteCollection : Microsoft.AspNetCore.Routing.IRouteCollection, Microsoft.AspNetCore.Routing.IRouter
    {
        public RouteCollection() { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Routing.IRouter this[int index] { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Routing.IRouter router) { }
        public virtual Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    public partial class RouteConstraintBuilder
    {
        public RouteConstraintBuilder(Microsoft.AspNetCore.Routing.IInlineConstraintResolver inlineConstraintResolver, string displayName) { }
        public void AddConstraint(string key, object value) { }
        public void AddResolvedConstraint(string key, string constraintText) { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> Build() { throw null; }
        public void SetOptional(string key) { }
    }
    public static partial class RouteConstraintMatcher
    {
        public static bool Match(System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> constraints, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, Microsoft.AspNetCore.Routing.RouteDirection routeDirection, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
    public partial class RouteCreationException : System.Exception
    {
        public RouteCreationException(string message) { }
        public RouteCreationException(string message, System.Exception innerException) { }
    }
    public sealed partial class RouteEndpoint : Microsoft.AspNetCore.Http.Endpoint
    {
        public RouteEndpoint(Microsoft.AspNetCore.Http.RequestDelegate requestDelegate, Microsoft.AspNetCore.Routing.Patterns.RoutePattern routePattern, int order, Microsoft.AspNetCore.Http.EndpointMetadataCollection metadata, string displayName) : base (default(Microsoft.AspNetCore.Http.RequestDelegate), default(Microsoft.AspNetCore.Http.EndpointMetadataCollection), default(string)) { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePattern RoutePattern { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class RouteEndpointBuilder : Microsoft.AspNetCore.Builder.EndpointBuilder
    {
        public RouteEndpointBuilder(Microsoft.AspNetCore.Http.RequestDelegate requestDelegate, Microsoft.AspNetCore.Routing.Patterns.RoutePattern routePattern, int order) { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePattern RoutePattern { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.Http.Endpoint Build() { throw null; }
    }
    public partial class RouteHandler : Microsoft.AspNetCore.Routing.IRouteHandler, Microsoft.AspNetCore.Routing.IRouter
    {
        public RouteHandler(Microsoft.AspNetCore.Http.RequestDelegate requestDelegate) { }
        public Microsoft.AspNetCore.Http.RequestDelegate GetRequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData) { throw null; }
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public sealed partial class RouteNameMetadata : Microsoft.AspNetCore.Routing.IRouteNameMetadata
    {
        public RouteNameMetadata(string routeName) { }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class RouteOptions
    {
        public RouteOptions() { }
        public bool AppendTrailingSlash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IDictionary<string, System.Type> ConstraintMap { get { throw null; } set { } }
        public bool LowercaseQueryStrings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool LowercaseUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressCheckForUnhandledSecurityMetadata { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RouteValueEqualityComparer : System.Collections.Generic.IEqualityComparer<object>
    {
        public static readonly Microsoft.AspNetCore.Routing.RouteValueEqualityComparer Default;
        public RouteValueEqualityComparer() { }
        public new bool Equals(object x, object y) { throw null; }
        public int GetHashCode(object obj) { throw null; }
    }
    public partial class RouteValuesAddress
    {
        public RouteValuesAddress() { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary AmbientValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary ExplicitValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RoutingFeature : Microsoft.AspNetCore.Routing.IRoutingFeature
    {
        public RoutingFeature() { }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public sealed partial class SuppressLinkGenerationMetadata : Microsoft.AspNetCore.Routing.ISuppressLinkGenerationMetadata
    {
        public SuppressLinkGenerationMetadata() { }
        public bool SuppressLinkGeneration { get { throw null; } }
    }
    public sealed partial class SuppressMatchingMetadata : Microsoft.AspNetCore.Routing.ISuppressMatchingMetadata
    {
        public SuppressMatchingMetadata() { }
        public bool SuppressMatching { get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Routing.Constraints
{
    public partial class AlphaRouteConstraint : Microsoft.AspNetCore.Routing.Constraints.RegexRouteConstraint
    {
        public AlphaRouteConstraint() : base (default(System.Text.RegularExpressions.Regex)) { }
    }
    public partial class BoolRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public BoolRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class CompositeRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public CompositeRouteConstraint(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.IRouteConstraint> constraints) { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.IRouteConstraint> Constraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class DateTimeRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public DateTimeRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class DecimalRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public DecimalRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class DoubleRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public DoubleRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class FileNameRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public FileNameRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class FloatRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public FloatRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class GuidRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public GuidRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class HttpMethodRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public HttpMethodRouteConstraint(params string[] allowedMethods) { }
        public System.Collections.Generic.IList<string> AllowedMethods { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class IntRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public IntRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class LengthRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public LengthRouteConstraint(int length) { }
        public LengthRouteConstraint(int minLength, int maxLength) { }
        public int MaxLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int MinLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class LongRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public LongRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class MaxLengthRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public MaxLengthRouteConstraint(int maxLength) { }
        public int MaxLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class MaxRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public MaxRouteConstraint(long max) { }
        public long Max { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class MinLengthRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public MinLengthRouteConstraint(int minLength) { }
        public int MinLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class MinRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public MinRouteConstraint(long min) { }
        public long Min { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class NonFileNameRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public NonFileNameRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class OptionalRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public OptionalRouteConstraint(Microsoft.AspNetCore.Routing.IRouteConstraint innerConstraint) { }
        public Microsoft.AspNetCore.Routing.IRouteConstraint InnerConstraint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class RangeRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public RangeRouteConstraint(long min, long max) { }
        public long Max { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public long Min { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class RegexInlineRouteConstraint : Microsoft.AspNetCore.Routing.Constraints.RegexRouteConstraint
    {
        public RegexInlineRouteConstraint(string regexPattern) : base (default(System.Text.RegularExpressions.Regex)) { }
    }
    public partial class RegexRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public RegexRouteConstraint(string regexPattern) { }
        public RegexRouteConstraint(System.Text.RegularExpressions.Regex regex) { }
        public System.Text.RegularExpressions.Regex Constraint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class RequiredRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public RequiredRouteConstraint() { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
    public partial class StringRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy, Microsoft.AspNetCore.Routing.IRouteConstraint
    {
        public StringRouteConstraint(string value) { }
        public bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Routing.Internal
{
    public partial class DfaGraphWriter
    {
        public DfaGraphWriter(System.IServiceProvider services) { }
        public void Write(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource, System.IO.TextWriter writer) { }
    }
}
namespace Microsoft.AspNetCore.Routing.Matching
{
    public sealed partial class CandidateSet
    {
        public CandidateSet(Microsoft.AspNetCore.Http.Endpoint[] endpoints, Microsoft.AspNetCore.Routing.RouteValueDictionary[] values, int[] scores) { }
        public int Count { get { throw null; } }
        public ref Microsoft.AspNetCore.Routing.Matching.CandidateState this[int index] { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]get { throw null; } }
        public void ExpandEndpoint(int index, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints, System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> comparer) { }
        public bool IsValidCandidate(int index) { throw null; }
        public void ReplaceEndpoint(int index, Microsoft.AspNetCore.Http.Endpoint endpoint, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { }
        public void SetValidity(int index, bool value) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CandidateState
    {
        private object _dummy;
        private int _dummyPrimitive;
        public Microsoft.AspNetCore.Http.Endpoint Endpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Score { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public sealed partial class EndpointMetadataComparer : System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>
    {
        internal EndpointMetadataComparer() { }
        int System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint>.Compare(Microsoft.AspNetCore.Http.Endpoint x, Microsoft.AspNetCore.Http.Endpoint y) { throw null; }
    }
    public abstract partial class EndpointMetadataComparer<TMetadata> : System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> where TMetadata : class
    {
        public static readonly Microsoft.AspNetCore.Routing.Matching.EndpointMetadataComparer<TMetadata> Default;
        protected EndpointMetadataComparer() { }
        public int Compare(Microsoft.AspNetCore.Http.Endpoint x, Microsoft.AspNetCore.Http.Endpoint y) { throw null; }
        protected virtual int CompareMetadata(TMetadata x, TMetadata y) { throw null; }
        protected virtual TMetadata GetMetadata(Microsoft.AspNetCore.Http.Endpoint endpoint) { throw null; }
    }
    public abstract partial class EndpointSelector
    {
        protected EndpointSelector() { }
        public abstract System.Threading.Tasks.Task SelectAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates);
    }
    public sealed partial class HostMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointComparerPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy, Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy
    {
        public HostMatcherPolicy() { }
        public System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> Comparer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
        public Microsoft.AspNetCore.Routing.Matching.PolicyJumpTable BuildJumpTable(int exitDestination, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyJumpTableEdge> edges) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyNodeEdge> GetEdges(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
    }
    public sealed partial class HttpMethodMatcherPolicy : Microsoft.AspNetCore.Routing.MatcherPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointComparerPolicy, Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy, Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy
    {
        public HttpMethodMatcherPolicy() { }
        public System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> Comparer { get { throw null; } }
        public override int Order { get { throw null; } }
        public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates) { throw null; }
        public Microsoft.AspNetCore.Routing.Matching.PolicyJumpTable BuildJumpTable(int exitDestination, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyJumpTableEdge> edges) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyNodeEdge> GetEdges(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        bool Microsoft.AspNetCore.Routing.Matching.INodeBuilderPolicy.AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
    }
    public partial interface IEndpointComparerPolicy
    {
        System.Collections.Generic.IComparer<Microsoft.AspNetCore.Http.Endpoint> Comparer { get; }
    }
    public partial interface IEndpointSelectorPolicy
    {
        bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints);
        System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.Matching.CandidateSet candidates);
    }
    public partial interface INodeBuilderPolicy
    {
        bool AppliesToEndpoints(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints);
        Microsoft.AspNetCore.Routing.Matching.PolicyJumpTable BuildJumpTable(int exitDestination, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyJumpTableEdge> edges);
        System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Matching.PolicyNodeEdge> GetEdges(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints);
    }
    public abstract partial class PolicyJumpTable
    {
        protected PolicyJumpTable() { }
        public abstract int GetDestination(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct PolicyJumpTableEdge
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public PolicyJumpTableEdge(object state, int destination) { throw null; }
        public int Destination { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object State { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct PolicyNodeEdge
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public PolicyNodeEdge(object state, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> endpoints) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.Endpoint> Endpoints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object State { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.Routing.Patterns
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePattern
    {
        internal RoutePattern() { }
        public static readonly object RequiredValueAny;
        public System.Collections.Generic.IReadOnlyDictionary<string, object> Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public decimal InboundPrecedence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public decimal OutboundPrecedence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference>> ParameterPolicies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> PathSegments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RawText { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, object> RequiredValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart GetParameter(string name) { throw null; }
    }
    public sealed partial class RoutePatternException : System.Exception
    {
        public RoutePatternException(string pattern, string message) { }
        public string Pattern { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public static partial class RoutePatternFactory
    {
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference Constraint(Microsoft.AspNetCore.Routing.IRouteConstraint constraint) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference Constraint(object constraint) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference Constraint(string constraint) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternLiteralPart LiteralPart(string content) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParameterPart(string parameterName) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParameterPart(string parameterName, object @default) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParameterPart(string parameterName, object @default, Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterKind parameterKind) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParameterPart(string parameterName, object @default, Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterKind parameterKind, params Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference[] parameterPolicies) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPart ParameterPart(string parameterName, object @default, Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterKind parameterKind, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference> parameterPolicies) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference ParameterPolicy(Microsoft.AspNetCore.Routing.IParameterPolicy parameterPolicy) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference ParameterPolicy(string parameterPolicy) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Parse(string pattern) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Parse(string pattern, object defaults, object parameterPolicies) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Parse(string pattern, object defaults, object parameterPolicies, object requiredValues) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(params Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment[] segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(object defaults, object parameterPolicies, params Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment[] segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(object defaults, object parameterPolicies, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(string rawText, params Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment[] segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(string rawText, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(string rawText, object defaults, object parameterPolicies, params Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment[] segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePattern Pattern(string rawText, object defaults, object parameterPolicies, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment> segments) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment Segment(params Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart[] parts) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment Segment(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart> parts) { throw null; }
        public static Microsoft.AspNetCore.Routing.Patterns.RoutePatternSeparatorPart SeparatorPart(string content) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternLiteralPart : Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart
    {
        internal RoutePatternLiteralPart() { }
        public string Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public enum RoutePatternParameterKind
    {
        Standard = 0,
        Optional = 1,
        CatchAll = 2,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternParameterPart : Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart
    {
        internal RoutePatternParameterPart() { }
        public object Default { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EncodeSlashes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsCatchAll { get { throw null; } }
        public bool IsOptional { get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterKind ParameterKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference> ParameterPolicies { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternParameterPolicyReference
    {
        internal RoutePatternParameterPolicyReference() { }
        public string Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.IParameterPolicy ParameterPolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class RoutePatternPart
    {
        internal RoutePatternPart() { }
        public bool IsLiteral { get { throw null; } }
        public bool IsParameter { get { throw null; } }
        public bool IsSeparator { get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePatternPartKind PartKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public enum RoutePatternPartKind
    {
        Literal = 0,
        Parameter = 1,
        Separator = 2,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternPathSegment
    {
        internal RoutePatternPathSegment() { }
        public bool IsSimple { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart> Parts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public sealed partial class RoutePatternSeparatorPart : Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart
    {
        internal RoutePatternSeparatorPart() { }
        public string Content { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class RoutePatternTransformer
    {
        protected RoutePatternTransformer() { }
        public abstract Microsoft.AspNetCore.Routing.Patterns.RoutePattern SubstituteRequiredValues(Microsoft.AspNetCore.Routing.Patterns.RoutePattern original, object requiredValues);
    }
}
namespace Microsoft.AspNetCore.Routing.Template
{
    public partial class InlineConstraint
    {
        public InlineConstraint(Microsoft.AspNetCore.Routing.Patterns.RoutePatternParameterPolicyReference other) { }
        public InlineConstraint(string constraint) { }
        public string Constraint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class RoutePrecedence
    {
        public static decimal ComputeInbound(Microsoft.AspNetCore.Routing.Template.RouteTemplate template) { throw null; }
        public static decimal ComputeOutbound(Microsoft.AspNetCore.Routing.Template.RouteTemplate template) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class RouteTemplate
    {
        public RouteTemplate(Microsoft.AspNetCore.Routing.Patterns.RoutePattern other) { }
        public RouteTemplate(string template, System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Template.TemplateSegment> segments) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.Template.TemplatePart> Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.Template.TemplateSegment> Segments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string TemplateText { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Template.TemplatePart GetParameter(string name) { throw null; }
        public Microsoft.AspNetCore.Routing.Template.TemplateSegment GetSegment(int index) { throw null; }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePattern ToRoutePattern() { throw null; }
    }
    public partial class TemplateBinder
    {
        internal TemplateBinder() { }
        public string BindValues(Microsoft.AspNetCore.Routing.RouteValueDictionary acceptedValues) { throw null; }
        public Microsoft.AspNetCore.Routing.Template.TemplateValuesResult GetValues(Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
        public static bool RoutePartsEqual(object a, object b) { throw null; }
        public bool TryProcessConstraints(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteValueDictionary combinedValues, out string parameterName, out Microsoft.AspNetCore.Routing.IRouteConstraint constraint) { throw null; }
    }
    public abstract partial class TemplateBinderFactory
    {
        protected TemplateBinderFactory() { }
        public abstract Microsoft.AspNetCore.Routing.Template.TemplateBinder Create(Microsoft.AspNetCore.Routing.Patterns.RoutePattern pattern);
        public abstract Microsoft.AspNetCore.Routing.Template.TemplateBinder Create(Microsoft.AspNetCore.Routing.Template.RouteTemplate template, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults);
    }
    public partial class TemplateMatcher
    {
        public TemplateMatcher(Microsoft.AspNetCore.Routing.Template.RouteTemplate template, Microsoft.AspNetCore.Routing.RouteValueDictionary defaults) { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Template.RouteTemplate Template { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool TryMatch(Microsoft.AspNetCore.Http.PathString path, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
    }
    public static partial class TemplateParser
    {
        public static Microsoft.AspNetCore.Routing.Template.RouteTemplate Parse(string routeTemplate) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class TemplatePart
    {
        public TemplatePart() { }
        public TemplatePart(Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart other) { }
        public object DefaultValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Template.InlineConstraint> InlineConstraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsCatchAll { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsLiteral { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsOptional { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsOptionalSeperator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool IsParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Text { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Routing.Template.TemplatePart CreateLiteral(string text) { throw null; }
        public static Microsoft.AspNetCore.Routing.Template.TemplatePart CreateParameter(string name, bool isCatchAll, bool isOptional, object defaultValue, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Routing.Template.InlineConstraint> inlineConstraints) { throw null; }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePatternPart ToRoutePatternPart() { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString()}")]
    public partial class TemplateSegment
    {
        public TemplateSegment() { }
        public TemplateSegment(Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment other) { }
        public bool IsSimple { get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Template.TemplatePart> Parts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Patterns.RoutePatternPathSegment ToRoutePatternPathSegment() { throw null; }
    }
    public partial class TemplateValuesResult
    {
        public TemplateValuesResult() { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary AcceptedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary CombinedValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Routing.Tree
{
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public partial class InboundMatch
    {
        public InboundMatch() { }
        public Microsoft.AspNetCore.Routing.Tree.InboundRouteEntry Entry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Template.TemplateMatcher TemplateMatcher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class InboundRouteEntry
    {
        public InboundRouteEntry() { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> Constraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.IRouter Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public decimal Precedence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Template.RouteTemplate RouteTemplate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class OutboundMatch
    {
        public OutboundMatch() { }
        public Microsoft.AspNetCore.Routing.Tree.OutboundRouteEntry Entry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Template.TemplateBinder TemplateBinder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class OutboundRouteEntry
    {
        public OutboundRouteEntry() { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Routing.IRouteConstraint> Constraints { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public object Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Defaults { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.IRouter Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public decimal Precedence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RequiredLinkValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Template.RouteTemplate RouteTemplate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TreeRouteBuilder
    {
        internal TreeRouteBuilder() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.Tree.InboundRouteEntry> InboundEntries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.Tree.OutboundRouteEntry> OutboundEntries { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Tree.TreeRouter Build() { throw null; }
        public Microsoft.AspNetCore.Routing.Tree.TreeRouter Build(int version) { throw null; }
        public void Clear() { }
        public Microsoft.AspNetCore.Routing.Tree.InboundRouteEntry MapInbound(Microsoft.AspNetCore.Routing.IRouter handler, Microsoft.AspNetCore.Routing.Template.RouteTemplate routeTemplate, string routeName, int order) { throw null; }
        public Microsoft.AspNetCore.Routing.Tree.OutboundRouteEntry MapOutbound(Microsoft.AspNetCore.Routing.IRouter handler, Microsoft.AspNetCore.Routing.Template.RouteTemplate routeTemplate, Microsoft.AspNetCore.Routing.RouteValueDictionary requiredLinkValues, string routeName, int order) { throw null; }
    }
    public partial class TreeRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        internal TreeRouter() { }
        public static readonly string RouteGroupKey;
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{DebuggerToString(),nq}")]
    public partial class UrlMatchingNode
    {
        public UrlMatchingNode(int length) { }
        public Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode CatchAlls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode ConstrainedCatchAlls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode ConstrainedParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Depth { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsCatchAll { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.Dictionary<string, Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode> Literals { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Routing.Tree.InboundMatch> Matches { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class UrlMatchingTree
    {
        public UrlMatchingTree(int order) { }
        public int Order { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.Tree.UrlMatchingNode Root { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RoutingServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddRouting(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddRouting(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Routing.RouteOptions> configureOptions) { throw null; }
    }
}
