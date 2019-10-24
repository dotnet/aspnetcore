// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.CookiePolicy
{
    internal class ResponseCookiesWrapper : IResponseCookies, ITrackingConsentFeature
    {
        private const string ConsentValue = "yes";
        private readonly ILogger _logger;
        private bool? _isConsentNeeded;
        private bool? _hasConsent;

        public ResponseCookiesWrapper(HttpContext context, CookiePolicyOptions options, IResponseCookiesFeature feature, ILogger logger)
        {
            Context = context;
            Feature = feature;
            Options = options;
            _logger = logger;
        }

        private HttpContext Context { get; }

        private IResponseCookiesFeature Feature { get; }

        private IResponseCookies Cookies => Feature.Cookies;

        private CookiePolicyOptions Options { get; }

        public bool IsConsentNeeded
        {
            get
            {
                if (!_isConsentNeeded.HasValue)
                {
                    _isConsentNeeded = Options.CheckConsentNeeded == null ? false
                        : Options.CheckConsentNeeded(Context);
                    _logger.NeedsConsent(_isConsentNeeded.Value);
                }

                return _isConsentNeeded.Value;
            }
        }

        public bool HasConsent
        {
            get
            {
                if (!_hasConsent.HasValue)
                {
                    var cookie = Context.Request.Cookies[Options.ConsentCookie.Name];
                    _hasConsent = string.Equals(cookie, ConsentValue, StringComparison.Ordinal);
                    _logger.HasConsent(_hasConsent.Value);
                }

                return _hasConsent.Value;
            }
        }

        public bool CanTrack => !IsConsentNeeded || HasConsent;

        public void GrantConsent()
        {
            if (!HasConsent && !Context.Response.HasStarted)
            {
                var cookieOptions = Options.ConsentCookie.Build(Context);
                // Note policy will be applied. We don't want to bypass policy because we want HttpOnly, Secure, etc. to apply.
                Append(Options.ConsentCookie.Name, ConsentValue, cookieOptions);
                _logger.ConsentGranted();
            }
            _hasConsent = true;
        }

        public void WithdrawConsent()
        {
            if (HasConsent && !Context.Response.HasStarted)
            {
                var cookieOptions = Options.ConsentCookie.Build(Context);
                // Note policy will be applied. We don't want to bypass policy because we want HttpOnly, Secure, etc. to apply.
                Delete(Options.ConsentCookie.Name, cookieOptions);
                _logger.ConsentWithdrawn();
            }
            _hasConsent = false;
        }

        // Note policy will be applied. We don't want to bypass policy because we want HttpOnly, Secure, etc. to apply.
        public string CreateConsentCookie()
        {
            var key = Options.ConsentCookie.Name;
            var value = ConsentValue;
            var options = Options.ConsentCookie.Build(Context);
            ApplyAppendPolicy(ref key, ref value, options);

            var setCookieHeaderValue = new Net.Http.Headers.SetCookieHeaderValue(
                Uri.EscapeDataString(key),
                Uri.EscapeDataString(value))
                {
                    Domain = options.Domain,
                    Path = options.Path,
                    Expires = options.Expires,
                    MaxAge = options.MaxAge,
                    Secure = options.Secure,
                    SameSite = (Net.Http.Headers.SameSiteMode)options.SameSite,
                    HttpOnly = options.HttpOnly
                };

            return setCookieHeaderValue.ToString();
        }

        private bool CheckPolicyRequired()
        {
            return !CanTrack
                || Options.MinimumSameSitePolicy != SameSiteMode.Unspecified
                || Options.HttpOnly != HttpOnlyPolicy.None
                || Options.Secure != CookieSecurePolicy.None;
        }

        public void Append(string key, string value)
        {
            if (CheckPolicyRequired() || Options.OnAppendCookie != null)
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

            if (ApplyAppendPolicy(ref key, ref value, options))
            {
                Cookies.Append(key, value, options);
            }
            else
            {
                _logger.CookieSuppressed(key);
            }
        }

        private bool ApplyAppendPolicy(ref string key, ref string value, CookieOptions options)
        {
            var issueCookie = CanTrack || options.IsEssential;
            ApplyPolicy(key, options);
            if (Options.OnAppendCookie != null)
            {
                var context = new AppendCookieContext(Context, options, key, value)
                {
                    IsConsentNeeded = IsConsentNeeded,
                    HasConsent = HasConsent,
                    IssueCookie = issueCookie,
                };
                Options.OnAppendCookie(context);

                key = context.CookieName;
                value = context.CookieValue;
                issueCookie = context.IssueCookie;
            }

            return issueCookie;
        }

        public void Delete(string key)
        {
            if (CheckPolicyRequired() || Options.OnDeleteCookie != null)
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

            // Assume you can always delete cookies unless directly overridden in the user event.
            var issueCookie = true;
            ApplyPolicy(key, options);
            if (Options.OnDeleteCookie != null)
            {
                var context = new DeleteCookieContext(Context, options, key)
                {
                    IsConsentNeeded = IsConsentNeeded,
                    HasConsent = HasConsent,
                    IssueCookie = issueCookie,
                };
                Options.OnDeleteCookie(context);

                key = context.CookieName;
                issueCookie = context.IssueCookie;
            }

            if (issueCookie)
            {
                Cookies.Delete(key, options);
            }
            else
            {
                _logger.DeleteCookieSuppressed(key);
            }
        }

        private void ApplyPolicy(string key, CookieOptions options)
        {
            switch (Options.Secure)
            {
                case CookieSecurePolicy.Always:
                    if (!options.Secure)
                    {
                        options.Secure = true;
                        _logger.CookieUpgradedToSecure(key);
                    }
                    break;
                case CookieSecurePolicy.SameAsRequest:
                    // Never downgrade a cookie
                    if (Context.Request.IsHttps && !options.Secure)
                    {
                        options.Secure = true;
                        _logger.CookieUpgradedToSecure(key);
                    }
                    break;
                case CookieSecurePolicy.None:
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (options.SameSite < Options.MinimumSameSitePolicy)
            {
                options.SameSite = Options.MinimumSameSitePolicy;
                _logger.CookieSameSiteUpgraded(key, Options.MinimumSameSitePolicy.ToString());
            }

            switch (Options.HttpOnly)
            {
                case HttpOnlyPolicy.Always:
                    if (!options.HttpOnly)
                    {
                        options.HttpOnly = true;
                        _logger.CookieUpgradedToHttpOnly(key);
                    }
                    break;
                case HttpOnlyPolicy.None:
                    break;
                default:
                    throw new InvalidOperationException($"Unrecognized {nameof(HttpOnlyPolicy)} value {Options.HttpOnly.ToString()}");
            }
        }
    }
}
