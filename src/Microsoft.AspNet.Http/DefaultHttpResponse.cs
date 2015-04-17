// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Collections;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http
{
    public class DefaultHttpResponse : HttpResponse
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpResponseFeature> _response = FeatureReference<IHttpResponseFeature>.Default;
        private FeatureReference<IResponseCookiesFeature> _cookies = FeatureReference<IResponseCookiesFeature>.Default;

        public DefaultHttpResponse(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpResponseFeature HttpResponseFeature
        {
            get { return _response.Fetch(_features); }
        }

        private IResponseCookiesFeature ResponseCookiesFeature
        {
            get { return _cookies.Fetch(_features) ?? _cookies.Update(_features, new ResponseCookiesFeature(_features)); }
        }

        public override HttpContext HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return HttpResponseFeature.StatusCode; }
            set { HttpResponseFeature.StatusCode = value; }
        }

        public override IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(HttpResponseFeature.Headers); }
        }

        public override Stream Body
        {
            get { return HttpResponseFeature.Body; }
            set { HttpResponseFeature.Body = value; }
        }

        public override long? ContentLength
        {
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
        }

        public override string ContentType
        {
            get
            {
                var contentType = Headers[HeaderNames.ContentType];
                return contentType;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    HttpResponseFeature.Headers.Remove(HeaderNames.ContentType);
                }
                else
                {
                    HttpResponseFeature.Headers[HeaderNames.ContentType] = new[] { value };
                }
            }
        }

        public override IResponseCookies Cookies
        {
            get { return ResponseCookiesFeature.Cookies; }
        }

        public override bool HeadersSent
        {
            get { return HttpResponseFeature.HeadersSent; }
        }

        public override void OnSendingHeaders(Action<object> callback, object state)
        {
            HttpResponseFeature.OnSendingHeaders(callback, state);
        }

        public override void OnResponseCompleted(Action<object> callback, object state)
        {
            HttpResponseFeature.OnResponseCompleted(callback, state);
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

            Headers.Set(HeaderNames.Location, location);
        }
    }
}