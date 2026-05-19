// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultHttpRequest : HttpRequest
{
    private const string Http = "http";
    private const string Https = "https";

    // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IHttpRequestFeature?> _nullRequestFeature = f => null;
    private static readonly Func<IFeatureCollection, IQueryFeature?> _newQueryFeature = f => new QueryFeature(f);
    private static readonly Func<DefaultHttpRequest, IFormFeature> _newFormFeature = r => new FormFeature(r, r._context.FormOptions ?? FormOptions.Default, r._context.GetEndpoint());
    private static readonly Func<IFeatureCollection, IRequestCookiesFeature> _newRequestCookiesFeature = f => new RequestCookiesFeature(f);
    private static readonly Func<IFeatureCollection, IRouteValuesFeature> _newRouteValuesFeature = f => new RouteValuesFeature();
    private static readonly Func<HttpContext, IRequestBodyPipeFeature> _newRequestBodyPipeFeature = context => new RequestBodyPipeFeature(context);

    private readonly DefaultHttpContext _context;
    private FeatureReferences<FeatureInterfaces> _features;

    public DefaultHttpRequest(DefaultHttpContext context)
    {
        _context = context;
        _features.Initalize(context.Features);
    }

    public void Initialize()
    {
        _features.Initalize(_context.Features);
    }

    public void Initialize(int revision)
    {
        _features.Initalize(_context.Features, revision);
    }

    public void Uninitialize()
    {
        _features = default;
    }

    public override HttpContext HttpContext => _context;

    private IHttpRequestFeature HttpRequestFeature =>
        _features.Fetch(ref _features.Cache.Request, _nullRequestFeature)!;

    private IQueryFeature QueryFeature =>
        _features.Fetch(ref _features.Cache.Query, _newQueryFeature)!;

    private IFormFeature FormFeature =>
        _features.Fetch(ref _features.Cache.Form, this, _newFormFeature)!;

    private IRequestCookiesFeature RequestCookiesFeature =>
        _features.Fetch(ref _features.Cache.Cookies, _newRequestCookiesFeature)!;

    private IRouteValuesFeature RouteValuesFeature =>
        _features.Fetch(ref _features.Cache.RouteValues, _newRouteValuesFeature)!;

    private IRequestBodyPipeFeature RequestBodyPipeFeature =>
        _features.Fetch(ref _features.Cache.BodyPipe, this.HttpContext, _newRequestBodyPipeFeature)!;

    public override PathString PathBase
    {
        get { return new PathString(HttpRequestFeature.PathBase); }
        set { HttpRequestFeature.PathBase = value.Value ?? string.Empty; }
    }

    public override PathString Path
    {
        get { return new PathString(HttpRequestFeature.Path); }
        set { HttpRequestFeature.Path = value.Value ?? string.Empty; }
    }

    public override QueryString QueryString
    {
        get { return new QueryString(HttpRequestFeature.QueryString); }
        set { HttpRequestFeature.QueryString = value.Value ?? string.Empty; }
    }

    public override long? ContentLength
    {
        get { return Headers.ContentLength; }
        set { Headers.ContentLength = value; }
    }

    public override Stream Body
    {
        get { return HttpRequestFeature.Body; }
        set { HttpRequestFeature.Body = value; }
    }

    public override string Method
    {
        get { return HttpRequestFeature.Method; }
        set { HttpRequestFeature.Method = value; }
    }

    public override string Scheme
    {
        get { return HttpRequestFeature.Scheme; }
        set { HttpRequestFeature.Scheme = value; }
    }

    public override bool IsHttps
    {
        get { return string.Equals(Https, Scheme, StringComparison.OrdinalIgnoreCase); }
        set { Scheme = value ? Https : Http; }
    }

    public override HostString Host
    {
        get { return HostString.FromUriComponent(Headers.Host.ToString()); }
        set { Headers.Host = value.ToUriComponent(); }
    }

    public override IQueryCollection Query
    {
        get { return QueryFeature.Query; }
        set { QueryFeature.Query = value; }
    }

    public override string Protocol
    {
        get { return HttpRequestFeature.Protocol; }
        set { HttpRequestFeature.Protocol = value; }
    }

    public override IHeaderDictionary Headers
    {
        get { return HttpRequestFeature.Headers; }
    }

    public override IRequestCookieCollection Cookies
    {
        get { return RequestCookiesFeature.Cookies; }
        set { RequestCookiesFeature.Cookies = value; }
    }

    public override string? ContentType
    {
        get { return Headers.ContentType; }
        set { Headers.ContentType = value; }
    }

    public override bool HasFormContentType
    {
        get { return FormFeature.HasFormContentType; }
    }

    public override IFormCollection Form
    {
        get { return FormFeature.ReadForm(); }
        set { FormFeature.Form = value; }
    }

    public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
    {
        return FormFeature.ReadFormAsync(cancellationToken);
    }

    public override RouteValueDictionary RouteValues
    {
        get { return RouteValuesFeature.RouteValues; }
        set { RouteValuesFeature.RouteValues = value; }
    }

    public override PipeReader BodyReader
    {
        get { return RequestBodyPipeFeature.Reader; }
    }

    struct FeatureInterfaces
    {
        public IHttpRequestFeature? Request;
        public IQueryFeature? Query;
        public IFormFeature? Form;
        public IRequestCookiesFeature? Cookies;
        public IRouteValuesFeature? RouteValues;
        public IRequestBodyPipeFeature? BodyPipe;
    }
}
