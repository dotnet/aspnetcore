// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Provides data from cookie to the current <see cref="ITempDataDictionary"/> object.
/// </summary>
public partial class CookieTempDataProvider : ITempDataProvider
{
    /// <summary>
    /// The name of the cookie.
    /// </summary>
    public static readonly string CookieName = ".AspNetCore.Mvc.CookieTempDataProvider";
    private const string Purpose = "Microsoft.AspNetCore.Mvc.CookieTempDataProviderToken.v1";

    private readonly IDataProtector _dataProtector;
    private readonly ILogger _logger;
    private readonly TempDataSerializer _tempDataSerializer;
    private readonly ChunkingCookieManager _chunkingCookieManager;
    private readonly CookieTempDataProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="CookieTempDataProvider"/>.
    /// </summary>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="options">The <see cref="CookieTempDataProviderOptions"/>.</param>
    /// <param name="tempDataSerializer">The <see cref="TempDataSerializer"/>.</param>
    public CookieTempDataProvider(
        IDataProtectionProvider dataProtectionProvider,
        ILoggerFactory loggerFactory,
        IOptions<CookieTempDataProviderOptions> options,
        TempDataSerializer tempDataSerializer)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = loggerFactory.CreateLogger<CookieTempDataProvider>();
        _tempDataSerializer = tempDataSerializer;
        _chunkingCookieManager = new ChunkingCookieManager();
        _options = options.Value;
    }

    /// <summary>
    /// Loads the temp data from the request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The temp data.</returns>
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
                    Log.TempDataCookieLoadSuccess(_logger, _options.Cookie.Name);
                    return tempData;
                }
            }
            catch (Exception ex)
            {
                Log.TempDataCookieLoadFailure(_logger, _options.Cookie.Name, ex);

                // If we've failed, we want to try and clear the cookie so that this won't keep happening
                // over and over.
                if (!context.Response.HasStarted)
                {
                    _chunkingCookieManager.DeleteCookie(context, _options.Cookie.Name, _options.Cookie.Build(context));
                }
            }
        }

        Log.TempDataCookieNotFound(_logger, _options.Cookie.Name);
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Save the temp data to the request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="values">The values.</param>
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

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "The temp data cookie {CookieName} was not found.", EventName = "TempDataCookieNotFound")]
        public static partial void TempDataCookieNotFound(ILogger logger, string cookieName);

        [LoggerMessage(2, LogLevel.Debug, "The temp data cookie {CookieName} was used to successfully load temp data.", EventName = "TempDataCookieLoadSuccess")]
        public static partial void TempDataCookieLoadSuccess(ILogger logger, string cookieName);

        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);
    }
}
