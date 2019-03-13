// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Internal
{
    public sealed class DefaultHttpResponse : HttpResponse
    {
        // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpResponseFeature> _nullResponseFeature = f => null;
        private readonly static Func<IFeatureCollection, IHttpResponseStartFeature> _nullResponseStartFeature = f => null;
        private readonly static Func<IFeatureCollection, IResponseCookiesFeature> _newResponseCookiesFeature = f => new ResponseCookiesFeature(f);
        private readonly static Func<HttpContext, IResponseBodyPipeFeature> _newResponseBodyPipeFeature = context => new ResponseBodyPipeFeature(context);

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
            _features.Fetch(ref _features.Cache.Response, _nullResponseFeature);

        private IHttpResponseStartFeature HttpResponseStartFeature =>
            _features.Fetch(ref _features.Cache.ResponseStart, _nullResponseStartFeature);

        private IResponseCookiesFeature ResponseCookiesFeature =>
            _features.Fetch(ref _features.Cache.Cookies, _newResponseCookiesFeature);

        private IResponseBodyPipeFeature ResponseBodyPipeFeature =>
            _features.Fetch(ref _features.Cache.BodyPipe, this.HttpContext, _newResponseBodyPipeFeature);

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
            get { return HttpResponseFeature.Body; }
            set { HttpResponseFeature.Body = value; }
        }

        public override long? ContentLength
        {
            get { return Headers.ContentLength; }
            set { Headers.ContentLength = value; }
        }

        public override string ContentType
        {
            get
            {
                return Headers[HeaderNames.ContentType];
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    HttpResponseFeature.Headers.Remove(HeaderNames.ContentType);
                }
                else
                {
                    HttpResponseFeature.Headers[HeaderNames.ContentType] = value;
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
            get { return ResponseBodyPipeFeature.Writer; }
            set { ResponseBodyPipeFeature.Writer = value; }
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

            Headers[HeaderNames.Location] = location;
        }

        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (HasStarted)
            {
                return Task.CompletedTask;
            }

            if (HttpResponseStartFeature == null)
            {
                return HttpResponseFeature.Body.FlushAsync(cancellationToken);
            }

            return HttpResponseStartFeature.StartAsync(cancellationToken);
        }

        struct FeatureInterfaces
        {
            public IHttpResponseFeature Response;
            public IResponseCookiesFeature Cookies;
            public IResponseBodyPipeFeature BodyPipe;
            public IHttpResponseStartFeature ResponseStart;
        }
    }
}
