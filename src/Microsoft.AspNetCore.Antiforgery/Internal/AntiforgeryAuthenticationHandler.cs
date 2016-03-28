// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class AntiforgeryAuthenticationHandler : IAuthenticationHandler
    {
        private readonly IAntiforgery _antiforgery;
        private HttpContext _httpContext;
        private IAuthenticationHandler _priorHandler;

        public AntiforgeryAuthenticationHandler(IAntiforgery antiforgery)
        {
            if (antiforgery == null)
            {
                throw new ArgumentNullException(nameof(antiforgery));
            }

            _antiforgery = antiforgery;
        }

        public async Task InitializeAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            _httpContext = httpContext;

            var authentication = GetAuthenticationFeature(_httpContext);

            _priorHandler = authentication.Handler;
            authentication.Handler = this;

            if (authentication.User != null)
            {
                if (!await _antiforgery.IsRequestValidAsync(_httpContext))
                {
                    // Wipe out any existing principal if we can't validate this request.
                    authentication.User = null;
                    return;
                }
            }
        }

        public void Teardown()
        {
            var authentication = GetAuthenticationFeature(_httpContext);
            authentication.Handler = _priorHandler;
        }

        /// <inheritdoc />
        public async Task AuthenticateAsync(AuthenticateContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_priorHandler != null)
            {
                await _priorHandler.AuthenticateAsync(context);

                var authentication = GetAuthenticationFeature(_httpContext);
                if (context.Principal != null)
                {
                    try
                    {
                        await _antiforgery.ValidateRequestAsync(_httpContext, context.Principal);
                    }
                    catch (AntiforgeryValidationException ex)
                    {
                        context.Failed(ex);
                        return;
                    }
                }
            }
        }

        /// <inheritdoc />
        public Task ChallengeAsync(ChallengeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_priorHandler != null)
            {
                return _priorHandler.ChallengeAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        /// <inheritdoc />
        public void GetDescriptions(DescribeSchemesContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_priorHandler != null)
            {
                _priorHandler.GetDescriptions(context);
            }
        }

        /// <inheritdoc />
        public Task SignInAsync(SignInContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_priorHandler != null)
            {
                return _priorHandler.SignInAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        /// <inheritdoc />
        public Task SignOutAsync(SignOutContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_priorHandler != null)
            {
                return _priorHandler.SignOutAsync(context);
            }

            return TaskCache.CompletedTask;
        }

        private static IHttpAuthenticationFeature GetAuthenticationFeature(HttpContext httpContext)
        {
            var authentication = httpContext.Features.Get<IHttpAuthenticationFeature>();
            if (authentication == null)
            {
                authentication = new HttpAuthenticationFeature();
                httpContext.Features.Set(authentication);
            }

            return authentication;
        }
    }
}
