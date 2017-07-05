// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
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
        private readonly TempDataSerializer _tempDataSerializer;
        private readonly ChunkingCookieManager _chunkingCookieManager;
        private readonly CookieTempDataProviderOptions _options;

        public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider, IOptions<CookieTempDataProviderOptions> options)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
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
                var encodedValue = _chunkingCookieManager.GetRequestCookie(context, _options.Cookie.Name);
                if (!string.IsNullOrEmpty(encodedValue))
                {
                    var protectedData = Base64UrlTextEncoder.Decode(encodedValue);
                    var unprotectedData = _dataProtector.Unprotect(protectedData);
                    return _tempDataSerializer.Deserialize(unprotectedData);
                }
            }

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
                var encodedValue = Base64UrlTextEncoder.Encode(bytes);
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
