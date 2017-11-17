// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.CookiePolicy
{
    public class CookiePolicyMiddleware
    {
        private readonly RequestDelegate _next;

        public CookiePolicyMiddleware(
            RequestDelegate next,
            IOptions<CookiePolicyOptions> options)
        {
            Options = options.Value;
            _next = next;
        }

        public CookiePolicyOptions Options { get; set; }

        public Task Invoke(HttpContext context)
        {
            var feature = context.Features.Get<IResponseCookiesFeature>() ?? new ResponseCookiesFeature(context.Features);
            var wrapper = new ResponseCookiesWrapper(context, Options, feature);
            context.Features.Set<IResponseCookiesFeature>(new CookiesWrapperFeature(wrapper));
            context.Features.Set<ITrackingConsentFeature>(wrapper);

            return _next(context);
        }

        private class CookiesWrapperFeature : IResponseCookiesFeature
        {
            public CookiesWrapperFeature(ResponseCookiesWrapper wrapper)
            {
                Cookies = wrapper;
            }

            public IResponseCookies Cookies { get; }
        }
    }
}