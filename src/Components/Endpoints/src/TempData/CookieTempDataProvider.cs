// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CookieTempDataProvider : ITempDataProvider
{
    public const string CookieName = ".AspNetCore.Components.TempData";
    private const string Purpose = "Microsoft.AspNetCore.Components.CookieTempDataProviderToken";
    private readonly IDataProtector _dataProtector;
    private readonly ITempDataSerializer _tempDataSerializer;
    private readonly CookieTempDataProviderOptions _options;
    private readonly ChunkingCookieManager _chunkingCookieManager;
    private readonly ILogger<CookieTempDataProvider> _logger;

    public CookieTempDataProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<CookieTempDataProviderOptions> options,
        ITempDataSerializer tempDataSerializer,
        ILogger<CookieTempDataProvider> logger)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _tempDataSerializer = tempDataSerializer;
        _options = options.Value;
        _chunkingCookieManager = new ChunkingCookieManager();
        _logger = logger;
    }

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        try
        {
            var cookieName = _options.Cookie.Name ?? CookieName;
            if (!context.Request.Cookies.ContainsKey(cookieName))
            {
                Log.TempDataCookieNotFound(_logger, cookieName);
                return new Dictionary<string, object?>();
            }
            var serializedDataFromCookie = _chunkingCookieManager.GetRequestCookie(context, cookieName);
            if (serializedDataFromCookie is null)
            {
                return new Dictionary<string, object?>();
            }

            var protectedBytes = WebEncoders.Base64UrlDecode(serializedDataFromCookie);
            var unprotectedBytes = _dataProtector.Unprotect(protectedBytes);
            var dataFromCookie = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(unprotectedBytes);

            if (dataFromCookie is null)
            {
                return new Dictionary<string, object?>();
            }

            var convertedData = new Dictionary<string, object?>();
            foreach (var kvp in dataFromCookie)
            {
                convertedData[kvp.Key] = _tempDataSerializer.Deserialize(kvp.Value);
            }
            Log.TempDataCookieLoadSuccess(_logger, cookieName);
            return convertedData;
        }
        catch (Exception ex)
        {
            var cookieName = _options.Cookie.Name ?? CookieName;
            Log.TempDataCookieLoadFailure(_logger, cookieName, ex);

            var cookieOptions = _options.Cookie.Build(context);
            SetCookiePath(context, cookieOptions);
            context.Response.Cookies.Delete(cookieName, cookieOptions);
            return new Dictionary<string, object?>();
        }
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
        foreach (var kvp in values)
        {
            if (!_tempDataSerializer.EnsureObjectCanBeSerialized(kvp.Value?.GetType() ?? typeof(object)))
            {
                throw new InvalidOperationException($"TempData cannot store values of type '{kvp.Value?.GetType()}'.");
            }
        }

        var cookieName = _options.Cookie.Name ?? CookieName;
        var cookieOptions = _options.Cookie.Build(context);
        SetCookiePath(context, cookieOptions);

        if (values.Count == 0)
        {
            _chunkingCookieManager.DeleteCookie(context, cookieName, cookieOptions);
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(values);
        var protectedBytes = _dataProtector.Protect(bytes);
        var encodedValue = WebEncoders.Base64UrlEncode(protectedBytes);
        _chunkingCookieManager.AppendResponseCookie(context, cookieName, encodedValue, cookieOptions);
        Log.TempDataCookieSaveSuccess(_logger, cookieName);
    }

    public void PersistExistingTempData(HttpContext context)
    {
        // No action needed since TempData is persisted automatically in cookies.
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

        [LoggerMessage(2, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "The temp data cookie {CookieName} was successfully saved.", EventName = "TempDataCookieSaveSuccess")]
        public static partial void TempDataCookieSaveSuccess(ILogger logger, string cookieName);

        [LoggerMessage(4, LogLevel.Debug, "The temp data cookie {CookieName} was successfully loaded.", EventName = "TempDataCookieLoadSuccess")]
        public static partial void TempDataCookieLoadSuccess(ILogger logger, string cookieName);
    }
}
