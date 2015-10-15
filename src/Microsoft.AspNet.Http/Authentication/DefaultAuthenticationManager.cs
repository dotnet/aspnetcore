// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Features.Authentication.Internal;

namespace Microsoft.AspNet.Http.Authentication.Internal
{
    public class DefaultAuthenticationManager : AuthenticationManager, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpAuthenticationFeature _authentication;
        private IHttpResponseFeature _response;

        public DefaultAuthenticationManager(IFeatureCollection features)
        {
            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _authentication = null;
                _response = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new HttpAuthenticationFeature(), 
                    ref _authentication);
            }
        }

        private IHttpResponseFeature HttpResponseFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _response); }
        }

        public override IEnumerable<AuthenticationDescription> GetAuthenticationSchemes()
        {
            var handler = HttpAuthenticationFeature.Handler;
            if (handler == null)
            {
                return new AuthenticationDescription[0];
            }

            var describeContext = new DescribeSchemesContext();
            handler.GetDescriptions(describeContext);
            return describeContext.Results.Select(description => new AuthenticationDescription(description));
        }

        public override async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var handler = HttpAuthenticationFeature.Handler;
            if (handler != null)
            {
                await handler.AuthenticateAsync(context);
            }

            if (!context.Accepted)
            {
                throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {context.AuthenticationScheme}");
            }
        }

        public override async Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            var handler = HttpAuthenticationFeature.Handler;

            var challengeContext = new ChallengeContext(authenticationScheme, properties?.Items, behavior);
            if (handler != null)
            {
                await handler.ChallengeAsync(challengeContext);
            }

            if (!challengeContext.Accepted)
            {
                throw new InvalidOperationException($"No authentication handler is configured to handle the scheme: {authenticationScheme}");
            }
        }

        public override async Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            var handler = HttpAuthenticationFeature.Handler;

            var signInContext = new SignInContext(authenticationScheme, principal, properties?.Items);
            if (handler != null)
            {
                await handler.SignInAsync(signInContext);
            }

            if (!signInContext.Accepted)
            {
                throw new InvalidOperationException($"No authentication handler is configured to handle the scheme: {authenticationScheme}");
            }
        }

        public override async Task SignOutAsync(string authenticationScheme, AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            var handler = HttpAuthenticationFeature.Handler;

            var signOutContext = new SignOutContext(authenticationScheme, properties?.Items);
            if (handler != null)
            {
                await handler.SignOutAsync(signOutContext);
            }

            if (!signOutContext.Accepted)
            {
                throw new InvalidOperationException($"No authentication handler is configured to handle the scheme: {authenticationScheme}");
            }
        }
    }
}
