// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Http.Core.Infrastructure;
using Microsoft.AspNet.Http.Core.Authentication;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.AspNet.Http.Interfaces.Authentication;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Http.Core
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

        public override bool HeadersSent
        {
            get { return HttpResponseFeature.HeadersSent; }
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

        public override void Challenge(AuthenticationProperties properties, [NotNull] IEnumerable<string> authenticationSchemes)
        {
            HttpResponseFeature.StatusCode = 401;
            var handler = HttpAuthenticationFeature.Handler;

            var challengeContext = new ChallengeContext(authenticationSchemes, properties == null ? null : properties.Dictionary);
            if (handler != null)
            {
                handler.Challenge(challengeContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationSchemes.Except(challengeContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication types were not accepted: " + string.Join(", ", leftovers));
            }
        }

        public override void SignIn(string authenticationScheme, [NotNull] ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var signInContext = new SignInContext(authenticationScheme, principal, properties == null ? null : properties.Dictionary);
            if (handler != null)
            {
                handler.SignIn(signInContext);
            }

            // Verify all types ack'd
            if (!signInContext.Accepted)
            {
                throw new InvalidOperationException("The following authentication scheme was not accepted: " + authenticationScheme);
            }
        }

        public override void SignOut(string authenticationScheme)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var signOutContext = new SignOutContext(authenticationScheme);
            if (handler != null)
            {
                handler.SignOut(signOutContext);
            }

            // Verify all types ack'd
            if (!string.IsNullOrWhiteSpace(authenticationScheme) && !signOutContext.Accepted)
            {
                throw new InvalidOperationException("The following authentication scheme was not accepted: " + authenticationScheme);
            }
        }
    }
}