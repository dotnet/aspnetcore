// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Authentication
{
    public class DefaultAuthenticationManager : AuthenticationManager
    {
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpAuthenticationFeature> _authentication = FeatureReference<IHttpAuthenticationFeature>.Default;
        private FeatureReference<IHttpResponseFeature> _response = FeatureReference<IHttpResponseFeature>.Default;

        public DefaultAuthenticationManager(IFeatureCollection features)
        {
            _features = features;
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new HttpAuthenticationFeature()); }
        }

        private IHttpResponseFeature HttpResponseFeature
        {
            get { return _response.Fetch(_features); }
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

        public override AuthenticationResult Authenticate([NotNull] string authenticationScheme)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationScheme);
            if (handler != null)
            {
                handler.Authenticate(authenticateContext);
            }

            if (!authenticateContext.Accepted)
            {
                throw new InvalidOperationException($"The following authentication scheme was not accepted: {authenticationScheme}");
            }

            if (authenticateContext.Principal == null)
            {
                return null;
            }

            return new AuthenticationResult(authenticateContext.Principal,
                new AuthenticationProperties(authenticateContext.Properties),
                new AuthenticationDescription(authenticateContext.Description));
        }

        public override async Task<AuthenticationResult> AuthenticateAsync([NotNull] string authenticationScheme)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationScheme);
            if (handler != null)
            {
                await handler.AuthenticateAsync(authenticateContext);
            }

            // Verify all types ack'd
            if (!authenticateContext.Accepted)
            {
                throw new InvalidOperationException($"The following authentication scheme was not accepted: {authenticationScheme}");
            }

            if (authenticateContext.Principal == null)
            {
                return null;
            }

            return new AuthenticationResult(authenticateContext.Principal,
                new AuthenticationProperties(authenticateContext.Properties),
                new AuthenticationDescription(authenticateContext.Description));
        }

        public override void Challenge(string authenticationScheme, AuthenticationProperties properties)
        {
            HttpResponseFeature.StatusCode = 401;
            var handler = HttpAuthenticationFeature.Handler;

            var challengeContext = new ChallengeContext(authenticationScheme, properties?.Items);
            if (handler != null)
            {
                handler.Challenge(challengeContext);
            }

            // The default Challenge with no scheme is always accepted
            if (!challengeContext.Accepted && !string.IsNullOrEmpty(authenticationScheme))
            {
                throw new InvalidOperationException($"The following authentication scheme was not accepted: {authenticationScheme}");
            }
        }

        public override void SignIn([NotNull] string authenticationScheme, [NotNull] ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var signInContext = new SignInContext(authenticationScheme, principal, properties?.Items);
            if (handler != null)
            {
                handler.SignIn(signInContext);
            }

            // Verify all types ack'd
            if (!signInContext.Accepted)
            {
                throw new InvalidOperationException($"The following authentication scheme was not accepted: {authenticationScheme}");
            }
        }

        public override void SignOut(string authenticationScheme, AuthenticationProperties properties)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var signOutContext = new SignOutContext(authenticationScheme, properties?.Items);
            if (handler != null)
            {
                handler.SignOut(signOutContext);
            }

            // Verify all types ack'd
            if (!string.IsNullOrWhiteSpace(authenticationScheme) && !signOutContext.Accepted)
            {
                throw new InvalidOperationException($"The following authentication scheme was not accepted: {authenticationScheme}");
            }
        }

        public override void SignOut(string authenticationScheme)
        {
            SignOut(authenticationScheme, properties: null);
        }
    }
}
