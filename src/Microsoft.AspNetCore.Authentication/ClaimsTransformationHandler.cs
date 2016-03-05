// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Handler that applies ClaimsTransformation to authentication
    /// </summary>
    public class ClaimsTransformationHandler : IAuthenticationHandler
    {
        private readonly IClaimsTransformer _transform;
        private readonly HttpContext _httpContext;

        public ClaimsTransformationHandler(IClaimsTransformer transform, HttpContext httpContext)
        {
            _transform = transform;
            _httpContext = httpContext;
        }

        public IAuthenticationHandler PriorHandler { get; set; }

        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (PriorHandler != null)
            {
                await PriorHandler.AuthenticateAsync(context);
                if (_transform != null && context?.Principal != null)
                {
                    var transformationContext = new ClaimsTransformationContext(_httpContext)
                    {
                        Principal = context.Principal
                    };
                    context.Authenticated(
                        await _transform.TransformAsync(transformationContext),
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
