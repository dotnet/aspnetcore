// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Handler that applies ClaimsTransformation to authentication
    /// </summary>
    public class ClaimsTransformationAuthenticationHandler : IAuthenticationHandler
    {
        private readonly Func<ClaimsPrincipal, ClaimsPrincipal> _transform;

        public ClaimsTransformationAuthenticationHandler(Func<ClaimsPrincipal, ClaimsPrincipal> transform)
        {
            _transform = transform;
        }

        public IAuthenticationHandler PriorHandler { get; set; }

        private void ApplyTransform(AuthenticateContext context)
        {
            if (_transform != null)
            {
                // REVIEW: this cast seems really bad (missing interface way to get the result back out?)
                var authContext = context as AuthenticateContext;
                if (authContext?.Principal != null)
                {
                    context.Authenticated(
                        _transform.Invoke(authContext.Principal),
                        authContext.Properties,
                        authContext.Description);
                }
            }

        }

        public void Authenticate(AuthenticateContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.Authenticate(context);
                ApplyTransform(context);
            }
        }

        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (PriorHandler != null)
            {
                await PriorHandler.AuthenticateAsync(context);
                ApplyTransform(context);
            }
        }

        public void Challenge(ChallengeContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.Challenge(context);
            }
        }

        public void GetDescriptions(DescribeSchemesContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(context);
            }
        }

        public void SignIn(SignInContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.SignIn(context);
            }
        }

        public void SignOut(SignOutContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.SignOut(context);
            }
        }

        public void RegisterAuthenticationHandler(IHttpAuthenticationFeature auth)
        {
            PriorHandler = auth.Handler;
            auth.Handler = this;
        }

        public void UnregisterAuthenticationHandler(IHttpAuthenticationFeature auth)
        {
            auth.Handler = PriorHandler;
        }

    }
}
