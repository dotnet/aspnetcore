// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Provides data from cookie to the current <see cref="ITempDataDictionary"/> object.
    /// </summary>
    public class CookieTempDataProvider : ITempDataProvider
    {
        public static readonly string CookieName = ".AspNetCore.Mvc.CookieTempDataProvider";
        private static readonly string Purpose = "Microsoft.AspNetCore.Mvc.CookieTempDataProviderToken.v1";

        private readonly IDataProtector _dataProtector;
        private readonly ILogger _logger;
        private readonly TempDataSerializer _tempDataSerializer;
        private readonly ChunkingCookieManager _chunkingCookieManager;
        private readonly CookieTempDataProviderOptions _options;

        public CookieTempDataProvider(
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            IOptions<CookieTempDataProviderOptions> options)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
            _logger = loggerFactory.CreateLogger<CookieTempDataProvider>();
            _tempDataSerializer = new TempDataSerializer();
            _chunkingCookieManager = new ChunkingCookieManager();
            _options = options.Value;
        }

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Request.Cookies.ContainsKey(_options.Cookie.Name))
            {
                // The cookie we use for temp data is user input, and might be invalid in many ways.
                //
                // Since TempData is a best-effort system, we don't want to throw and get a 500 if the cookie is
                // bad, we will just clear it and ignore the exception. The common case that we've identified for
                // this is misconfigured data protection settings, which can cause the key used to create the 
                // cookie to no longer be available.
                try
                {
                    var encodedValue = _chunkingCookieManager.GetRequestCookie(context, _options.Cookie.Name);
                    if (!string.IsNullOrEmpty(encodedValue))
                    {
                        var protectedData = WebEncoders.Base64UrlDecode(encodedValue);
                        var unprotectedData = _dataProtector.Unprotect(protectedData);
                        var tempData = _tempDataSerializer.Deserialize(unprotectedData);

                        _logger.TempDataCookieLoadSuccess(_options.Cookie.Name);
                        return tempData;
                    }
                }
                catch (Exception ex)
                {
                    _logger.TempDataCookieLoadFailure(_options.Cookie.Name, ex);

                    // If we've failed, we want to try and clear the cookie so that this won't keep happening
                    // over and over.
                    if (!context.Response.HasStarted)
                    {
                        _chunkingCookieManager.DeleteCookie(context, _options.Cookie.Name, _options.Cookie.Build(context));
                    }
                }
            }

            _logger.TempDataCookieNotFound(_options.Cookie.Name);
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cookieOptions = _options.Cookie.Build(context);
            SetCookiePath(context, cookieOptions);

            var hasValues = (values != null && values.Count > 0);
            if (hasValues)
            {
                var bytes = _tempDataSerializer.Serialize(values);
                bytes = _dataProtector.Protect(bytes);
                var encodedValue = WebEncoders.Base64UrlEncode(bytes);
                _chunkingCookieManager.AppendResponseCookie(context, _options.Cookie.Name, encodedValue, cookieOptions);
            }
            else
            {
                _chunkingCookieManager.DeleteCookie(context, _options.Cookie.Name, cookieOptions);
            }
        }

        private void SetCookiePath(HttpContext httpContext, CookieOptions cookieOptions)
        {
            if (!string.IsNullOrEmpty(_options.Cookie.Path))
            {
                cookieOptions.Path = _options.Cookie.Path;
            }
            else
            {
                var pathBase = httpContext.Request.PathBase.ToString();
                if (!string.IsNullOrEmpty(pathBase))
                {
                    cookieOptions.Path = pathBase;
                }
            }
        }
    }
}
