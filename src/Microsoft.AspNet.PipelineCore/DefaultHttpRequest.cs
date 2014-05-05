// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpRequest : HttpRequest
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;

        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private FeatureReference<IHttpConnectionFeature> _connection = FeatureReference<IHttpConnectionFeature>.Default;
        private FeatureReference<IHttpTransportLayerSecurityFeature> _transportLayerSecurity = FeatureReference<IHttpTransportLayerSecurityFeature>.Default;
        private FeatureReference<IQueryFeature> _query = FeatureReference<IQueryFeature>.Default;
        private FeatureReference<IFormFeature> _form = FeatureReference<IFormFeature>.Default;
        private FeatureReference<IRequestCookiesFeature> _cookies = FeatureReference<IRequestCookiesFeature>.Default;

        public DefaultHttpRequest(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return _request.Fetch(_features); }
        }

        private IHttpConnectionFeature HttpConnectionFeature
        {
            get { return _connection.Fetch(_features); }
        }

        private IHttpTransportLayerSecurityFeature HttpTransportLayerSecurityFeature
        {
            get { return _transportLayerSecurity.Fetch(_features); }
        }

        private IQueryFeature QueryFeature
        {
            get { return _query.Fetch(_features) ?? _query.Update(_features, new QueryFeature(_features)); }
        }

        private IFormFeature FormFeature
        {
            get { return _form.Fetch(_features) ?? _form.Update(_features, new FormFeature(_features)); }
        }

        private IRequestCookiesFeature RequestCookiesFeature
        {
            get { return _cookies.Fetch(_features) ?? _cookies.Update(_features, new RequestCookiesFeature(_features)); }
        }

        public override HttpContext HttpContext { get { return _context; } }

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
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
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

        public override bool IsSecure
        {
            get { return string.Equals("https", Scheme, StringComparison.OrdinalIgnoreCase); }
        }

        public override HostString Host
        {
            get { return HostString.FromUriComponent(Headers["Host"]); }
            set { Headers["Host"] = value.ToUriComponent(); }
        }

        public override IReadableStringCollection Query
        {
            get { return QueryFeature.Query; }
        }

        public override Task<IReadableStringCollection> GetFormAsync()
        {
            return FormFeature.GetFormAsync();
        }

        public override string Protocol
        {
            get { return HttpRequestFeature.Protocol; }
            set { HttpRequestFeature.Protocol = value; }
        }

        public override IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(HttpRequestFeature.Headers); }
        }

        public override IReadableStringCollection Cookies
        {
            get { return RequestCookiesFeature.Cookies; }
        }

        public override System.Threading.CancellationToken CallCanceled
        {
            get
            {
                // TODO: Which feature exposes this?
                return CancellationToken.None;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}