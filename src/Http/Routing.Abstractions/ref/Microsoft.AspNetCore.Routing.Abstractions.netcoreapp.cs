// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    public partial interface IOutboundParameterTransformer : Microsoft.AspNetCore.Routing.IParameterPolicy
    {
        string TransformOutbound(object value);
    }
    public partial interface IParameterPolicy
    {
    }
    public partial interface IRouteConstraint : Microsoft.AspNetCore.Routing.IParameterPolicy
    {
        bool Match(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.IRouter route, string routeKey, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteDirection routeDirection);
    }
    public partial interface IRouteHandler
    {
        Microsoft.AspNetCore.Http.RequestDelegate GetRequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteData routeData);
    }
    public partial interface IRouter
    {
        Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context);
        System.Threading.Tasks.Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context);
    }
    public partial interface IRoutingFeature
    {
        Microsoft.AspNetCore.Routing.RouteData RouteData { get; set; }
    }
    public abstract partial class LinkGenerator
    {
        protected LinkGenerator() { }
        public abstract string GetPathByAddress<TAddress>(Microsoft.AspNetCore.Http.HttpContext httpContext, TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues = null, Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null);
        public abstract string GetPathByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null);
        public abstract string GetUriByAddress<TAddress>(Microsoft.AspNetCore.Http.HttpContext httpContext, TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues = null, string scheme = null, Microsoft.AspNetCore.Http.HostString? host = default(Microsoft.AspNetCore.Http.HostString?), Microsoft.AspNetCore.Http.PathString? pathBase = default(Microsoft.AspNetCore.Http.PathString?), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null);
        public abstract string GetUriByAddress<TAddress>(TAddress address, Microsoft.AspNetCore.Routing.RouteValueDictionary values, string scheme, Microsoft.AspNetCore.Http.HostString host, Microsoft.AspNetCore.Http.PathString pathBase = default(Microsoft.AspNetCore.Http.PathString), Microsoft.AspNetCore.Http.FragmentString fragment = default(Microsoft.AspNetCore.Http.FragmentString), Microsoft.AspNetCore.Routing.LinkOptions options = null);
    }
    public partial class LinkOptions
    {
        public LinkOptions() { }
        public bool? AppendTrailingSlash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? LowercaseQueryStrings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool? LowercaseUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RouteContext
    {
        public RouteContext(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
        public Microsoft.AspNetCore.Http.RequestDelegate Handler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData RouteData { get { throw null; } set { } }
    }
    public partial class RouteData
    {
        public RouteData() { }
        public RouteData(Microsoft.AspNetCore.Routing.RouteData other) { }
        public RouteData(Microsoft.AspNetCore.Routing.RouteValueDictionary values) { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary DataTokens { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.IRouter> Routers { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Values { get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteData.RouteDataSnapshot PushState(Microsoft.AspNetCore.Routing.IRouter router, Microsoft.AspNetCore.Routing.RouteValueDictionary values, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public readonly partial struct RouteDataSnapshot
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public RouteDataSnapshot(Microsoft.AspNetCore.Routing.RouteData routeData, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens, System.Collections.Generic.IList<Microsoft.AspNetCore.Routing.IRouter> routers, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { throw null; }
            public void Restore() { }
        }
    }
    public enum RouteDirection
    {
        IncomingRequest = 0,
        UrlGeneration = 1,
    }
    public static partial class RoutingHttpContextExtensions
    {
        public static Microsoft.AspNetCore.Routing.RouteData GetRouteData(this Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public static object GetRouteValue(this Microsoft.AspNetCore.Http.HttpContext httpContext, string key) { throw null; }
    }
    public partial class VirtualPathContext
    {
        public VirtualPathContext(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues, Microsoft.AspNetCore.Routing.RouteValueDictionary values) { }
        public VirtualPathContext(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.AspNetCore.Routing.RouteValueDictionary ambientValues, Microsoft.AspNetCore.Routing.RouteValueDictionary values, string routeName) { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary AmbientValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RouteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary Values { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class VirtualPathData
    {
        public VirtualPathData(Microsoft.AspNetCore.Routing.IRouter router, string virtualPath) { }
        public VirtualPathData(Microsoft.AspNetCore.Routing.IRouter router, string virtualPath, Microsoft.AspNetCore.Routing.RouteValueDictionary dataTokens) { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary DataTokens { get { throw null; } }
        public Microsoft.AspNetCore.Routing.IRouter Router { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string VirtualPath { get { throw null; } set { } }
    }
}
