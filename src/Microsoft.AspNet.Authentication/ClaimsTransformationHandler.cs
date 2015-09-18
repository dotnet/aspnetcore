// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features.Authentication;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Handler that applies ClaimsTransformation to authentication
    /// </summary>
    public class ClaimsTransformationHandler : IAuthenticationHandler
    {
        private readonly IClaimsTransformer _transform;

        public ClaimsTransformationHandler(IClaimsTransformer transform)
        {
            _transform = transform;
        }

        public IAuthenticationHandler PriorHandler { get; set; }

        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (PriorHandler != null)
            {
                await PriorHandler.AuthenticateAsync(context);
                if (_transform != null && context?.Principal != null)
                {
                    context.Authenticated(
                        await _transform.TransformAsync(context.Principal),
                        context.Properties,
                        context.Description);
                }
            }
        }

        public Task ChallengeAsync(ChallengeContext context)
        {
            if (PriorHandler != null)
            {
                return PriorHandler.ChallengeAsync(context);
            }
            return Task.FromResult(0);
        }

        public void GetDescriptions(DescribeSchemesContext context)
        {
            if (PriorHandler != null)
            {
                PriorHandler.GetDescriptions(context);
            }
        }

        public Task SignInAsync(SignInContext context)
        {
            if (PriorHandler != null)
            {
                return PriorHandler.SignInAsync(context);
            }
            return Task.FromResult(0);
        }

        public Task SignOutAsync(SignOutContext context)
        {
            if (PriorHandler != null)
            {
                return PriorHandler.SignOutAsync(context);
            }
            return Task.FromResult(0);
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
