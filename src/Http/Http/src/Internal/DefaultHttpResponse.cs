// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultHttpResponse : HttpResponse
{
    // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IHttpResponseFeature?> _nullResponseFeature = f => null;
    private static readonly Func<IFeatureCollection, IHttpResponseBodyFeature?> _nullResponseBodyFeature = f => null;
    private static readonly Func<IFeatureCollection, IResponseCookiesFeature?> _newResponseCookiesFeature = f => new ResponseCookiesFeature(f);

    private readonly DefaultHttpContext _context;
    private FeatureReferences<FeatureInterfaces> _features;

    public DefaultHttpResponse(DefaultHttpContext context)
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

    private IHttpResponseFeature HttpResponseFeature =>
        _features.Fetch(ref _features.Cache.Response, _nullResponseFeature)!;

    private IHttpResponseBodyFeature HttpResponseBodyFeature =>
        _features.Fetch(ref _features.Cache.ResponseBody, _nullResponseBodyFeature)!;

    private IResponseCookiesFeature ResponseCookiesFeature =>
        _features.Fetch(ref _features.Cache.Cookies, _newResponseCookiesFeature)!;

    public override HttpContext HttpContext { get { return _context; } }

    public override int StatusCode
    {
        get { return HttpResponseFeature.StatusCode; }
        set { HttpResponseFeature.StatusCode = value; }
    }

    public override IHeaderDictionary Headers
    {
        get { return HttpResponseFeature.Headers; }
    }

    public override Stream Body
    {
        get { return HttpResponseBodyFeature.Stream; }
        set
        {
            var otherFeature = _features.Collection.GetRequiredFeature<IHttpResponseBodyFeature>();

            if (otherFeature is StreamResponseBodyFeature streamFeature
                && streamFeature.PriorFeature != null
                && object.ReferenceEquals(value, streamFeature.PriorFeature.Stream))
            {
                // They're reverting the stream back to the prior one. Revert the whole feature.
                _features.Collection.Set(streamFeature.PriorFeature);
                return;
            }

            _features.Collection.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(value, otherFeature));
        }
    }

    public override long? ContentLength
    {
        get { return Headers.ContentLength; }
        set { Headers.ContentLength = value; }
    }

    public override string? ContentType
    {
        get
        {
            return Headers.ContentType;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                HttpResponseFeature.Headers.ContentType = default;
            }
            else
            {
                HttpResponseFeature.Headers.ContentType = value;
            }
        }
    }

    public override IResponseCookies Cookies
    {
        get { return ResponseCookiesFeature.Cookies; }
    }

    public override bool HasStarted
    {
        get { return HttpResponseFeature.HasStarted; }
    }

    public override PipeWriter BodyWriter
    {
        get { return HttpResponseBodyFeature.Writer; }
    }

    public override void OnStarting(Func<object, Task> callback, object state)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        HttpResponseFeature.OnStarting(callback, state);
    }

    public override void OnCompleted(Func<object, Task> callback, object state)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        HttpResponseFeature.OnCompleted(callback, state);
    }

    public override void Redirect(string location, bool permanent)
    {
        if (permanent)
        {
            HttpResponseFeature.StatusCode = 301;
        }
        else
        {
            HttpResponseFeature.StatusCode = 302;
        }

        Headers.Location = location;
    }

    public override Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (HasStarted)
        {
            return Task.CompletedTask;
        }

        return HttpResponseBodyFeature.StartAsync(cancellationToken);
    }

    public override Task CompleteAsync() => HttpResponseBodyFeature.CompleteAsync();

    struct FeatureInterfaces
    {
        public IHttpResponseFeature? Response;
        public IHttpResponseBodyFeature? ResponseBody;
        public IResponseCookiesFeature? Cookies;
    }
}
