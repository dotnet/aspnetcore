// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            context.Features.Set<IResponseCookiesFeature>(new CookiesWrapperFeature(context, Options, feature));
            return _next(context);
        }

        private class CookiesWrapperFeature : IResponseCookiesFeature
        {
            public CookiesWrapperFeature(HttpContext context, CookiePolicyOptions options, IResponseCookiesFeature feature)
            {
                Wrapper = new CookiesWrapper(context, options, feature);
            }

            public IResponseCookies Wrapper { get; }

            public IResponseCookies Cookies
            {
                get
                {
                    return Wrapper;
                }
            }
        }

        private class CookiesWrapper : IResponseCookies
        {
            public CookiesWrapper(HttpContext context, CookiePolicyOptions options, IResponseCookiesFeature feature)
            {
                Context = context;
                Feature = feature;
                Policy = options;
            }

            public HttpContext Context { get; }

            public IResponseCookiesFeature Feature { get; }

            public IResponseCookies Cookies
            {
                get
                {
                    return Feature.Cookies;
                }
            }

            public CookiePolicyOptions Policy { get; }

            private bool PolicyRequiresCookieOptions()
            {
                return Policy.MinimumSameSitePolicy != SameSiteMode.None || Policy.HttpOnly != HttpOnlyPolicy.None || Policy.Secure != CookieSecurePolicy.None;
            }

            public void Append(string key, string value)
            {
                if (PolicyRequiresCookieOptions() || Policy.OnAppendCookie != null)
                {
                    Append(key, value, new CookieOptions());
                }
                else
                {
                    Cookies.Append(key, value);
                }
            }

            public void Append(string key, string value, CookieOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                ApplyPolicy(options);
                if (Policy.OnAppendCookie != null)
                {
                    var context = new AppendCookieContext(Context, options, key, value);
                    Policy.OnAppendCookie(context);
                    key = context.CookieName;
                    value = context.CookieValue;
                }
                Cookies.Append(key, value, options);
            }

            public void Delete(string key)
            {
                if (PolicyRequiresCookieOptions() || Policy.OnDeleteCookie != null)
                {
                    Delete(key, new CookieOptions());
                }
                else
                {
                    Cookies.Delete(key);
                }
            }

            public void Delete(string key, CookieOptions options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                ApplyPolicy(options);
                if (Policy.OnDeleteCookie != null)
                {
                    var context = new DeleteCookieContext(Context, options, key);
                    Policy.OnDeleteCookie(context);
                    key = context.CookieName;
                }
                Cookies.Delete(key, options);
            }

            private void ApplyPolicy(CookieOptions options)
            {
                switch (Policy.Secure)
                {
                    case CookieSecurePolicy.Always:
                        options.Secure = true;
                        break;
                    case CookieSecurePolicy.SameAsRequest:
                        options.Secure = Context.Request.IsHttps;
                        break;
                    case CookieSecurePolicy.None:
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                switch (Policy.MinimumSameSitePolicy)
                {
                    case SameSiteMode.None:
                        break;
                    case SameSiteMode.Lax:
                        if (options.SameSite == SameSiteMode.None)
                        {
                            options.SameSite = SameSiteMode.Lax;
                        }
                        break;
                    case SameSiteMode.Strict:
                        options.SameSite = SameSiteMode.Strict;
                        break;
                    default:
                        throw new InvalidOperationException($"Unrecognized {nameof(SameSiteMode)} value {Policy.MinimumSameSitePolicy.ToString()}");
                }
                switch (Policy.HttpOnly)
                {
                    case HttpOnlyPolicy.Always:
                        options.HttpOnly = true;
                        break;
                    case HttpOnlyPolicy.None:
                        break;
                    default:
                        throw new InvalidOperationException($"Unrecognized {nameof(HttpOnlyPolicy)} value {Policy.HttpOnly.ToString()}");
                }
            }
        }
    }
}