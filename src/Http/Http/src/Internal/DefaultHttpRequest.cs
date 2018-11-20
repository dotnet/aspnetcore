// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class DefaultHttpRequest : HttpRequest
    {
        // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = f => null;
        private readonly static Func<IFeatureCollection, IQueryFeature> _newQueryFeature = f => new QueryFeature(f);
        private readonly static Func<HttpRequest, IFormFeature> _newFormFeature = r => new FormFeature(r);
        private readonly static Func<IFeatureCollection, IRequestCookiesFeature> _newRequestCookiesFeature = f => new RequestCookiesFeature(f);

        private HttpContext _context;
        private FeatureReferences<FeatureInterfaces> _features;

        public DefaultHttpRequest(HttpContext context)
        {
            Initialize(context);
        }

        public virtual void Initialize(HttpContext context)
        {
            _context = context;
            _features = new FeatureReferences<FeatureInterfaces>(context.Features);
        }

        public virtual void Uninitialize()
        {
            _context = null;
            _features = default(FeatureReferences<FeatureInterfaces>);
        }

        public override HttpContext HttpContext => _context;

        private IHttpRequestFeature HttpRequestFeature =>
            _features.Fetch(ref _features.Cache.Request, _nullRequestFeature);

        private IQueryFeature QueryFeature =>
            _features.Fetch(ref _features.Cache.Query, _newQueryFeature);

        private IFormFeature FormFeature =>
            _features.Fetch(ref _features.Cache.Form, this, _newFormFeature);

        private IRequestCookiesFeature RequestCookiesFeature =>
            _features.Fetch(ref _features.Cache.Cookies, _newRequestCookiesFeature);

        public override PathString PathBase
        {
            get { return new PathString(HttpRequestFeature.PathBase); }
            set { HttpRequestFeature.PathBase = value.Value; }
        }

        public override PathString Path
        {
            get { return new PathString(HttpRequestFeature.Path); }
            set { HttpRequestFeature.Path = value.Value; }
        }

        public override QueryString QueryString
        {
            get { return new QueryString(HttpRequestFeature.QueryString); }
            set { HttpRequestFeature.QueryString = value.Value; }
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
            get { return string.Equals(Constants.Https, Scheme, StringComparison.OrdinalIgnoreCase); }
            set { Scheme = value ? Constants.Https : Constants.Http; }
        }

        public override HostString Host
        {
            get { return HostString.FromUriComponent(Headers["Host"]); }
            set { Headers["Host"] = value.ToUriComponent(); }
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

        public override string ContentType
        {
            get { return Headers[HeaderNames.ContentType]; }
            set { Headers[HeaderNames.ContentType] = value; }
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

        struct FeatureInterfaces
        {
            public IHttpRequestFeature Request;
            public IQueryFeature Query;
            public IFormFeature Form;
            public IRequestCookiesFeature Cookies;
        }
    }
}