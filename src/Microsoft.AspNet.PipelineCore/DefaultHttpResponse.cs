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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;
using Microsoft.AspNet.PipelineCore.Security;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpResponse : HttpResponse
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpResponseFeature> _response = FeatureReference<IHttpResponseFeature>.Default;
        private FeatureReference<IResponseCookiesFeature> _cookies = FeatureReference<IResponseCookiesFeature>.Default;
        private FeatureReference<IHttpAuthenticationFeature> _authentication = FeatureReference<IHttpAuthenticationFeature>.Default;

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

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new HttpAuthenticationFeature()); }
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
                var contentType = Headers[Constants.Headers.ContentType];
                return contentType;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    HttpResponseFeature.Headers.Remove(Constants.Headers.ContentType);
                }
                else
                {
                    HttpResponseFeature.Headers[Constants.Headers.ContentType] = new[] { value };
                }
            }
        }

        public override IResponseCookies Cookies
        {
            get { return ResponseCookiesFeature.Cookies; }
        }

        public override void OnSendingHeaders(Action<object> callback, object state)
        {
            HttpResponseFeature.OnSendingHeaders(callback, state);
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

            Headers.Set(Constants.Headers.Location, location);
        }

        public override Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Body.WriteAsync(bytes, 0, bytes.Length);
        }

        public override void Challenge(IList<string> authenticationTypes, AuthenticationProperties properties)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException();
            }
            HttpResponseFeature.StatusCode = 401;
            var handler = HttpAuthenticationFeature.Handler;

            var challengeContext = new ChallengeContext(authenticationTypes, properties == null ? null : properties.Dictionary);
            if (handler != null)
            {
                handler.Challenge(challengeContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(challengeContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }
        }

        public override void SignIn(IList<ClaimsIdentity> identities, AuthenticationProperties properties)
        {
            if (identities == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthenticationFeature.Handler;

            var signInContext = new SignInContext(identities, properties == null ? null : properties.Dictionary);
            if (handler != null)
            {
                handler.SignIn(signInContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = identities.Select(identity => identity.AuthenticationType).Except(signInContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }
        }

        public override void SignOut(IList<string> authenticationTypes)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException();
            }
            var handler = HttpAuthenticationFeature.Handler;

            var signOutContext = new SignOutContext(authenticationTypes);
            if (handler != null)
            {
                handler.SignOut(signOutContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationTypes.Except(signOutContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }
        }
    }
}