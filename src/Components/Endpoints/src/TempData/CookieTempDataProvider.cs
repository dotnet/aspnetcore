// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CookieTempDataProvider : ITempDataProvider
{
    public const string CookieName = ".AspNetCore.Components.TempData";
    private const string Purpose = "Microsoft.AspNetCore.Components.CookieTempDataProviderToken";
    private const int MaxEncodedLength = 4050;
    private readonly IDataProtector _dataProtector;
    private readonly ITempDataSerializer _tempDataSerializer;
    private readonly CookieTempDataProviderOptions _options;

    public CookieTempDataProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<CookieTempDataProviderOptions> options,
        ITempDataSerializer tempDataSerializer)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _tempDataSerializer = tempDataSerializer;
        _options = options.Value;
    }

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        try
        {
            var cookieName = _options.Cookie.Name ?? CookieName;
            var serializedDataFromCookie = context.Request.Cookies[cookieName];
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
            return convertedData;
        }
        catch (Exception ex)
        {
            var cookieName = _options.Cookie.Name ?? CookieName;
            if (context.RequestServices.GetService<ILogger<CookieTempDataProvider>>() is { } logger)
            {
                Log.TempDataCookieLoadFailure(logger, cookieName, ex);
            }

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
            context.Response.Cookies.Delete(cookieName, cookieOptions);
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(values);
        var protectedBytes = _dataProtector.Protect(bytes);
        var encodedValue = WebEncoders.Base64UrlEncode(protectedBytes);

        if (encodedValue.Length > MaxEncodedLength)
        {
            if (context.RequestServices.GetService<ILogger<CookieTempDataProvider>>() is { } logger)
            {
                Log.TempDataCookieSaveFailure(logger, cookieName);
            }

            context.Response.Cookies.Delete(cookieName, cookieOptions);
            return;
        }

        context.Response.Cookies.Append(cookieName, encodedValue, cookieOptions);
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
        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);

        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be saved, because it is too large to fit in a single cookie.", EventName = "TempDataCookieSaveFailure")]
        public static partial void TempDataCookieSaveFailure(ILogger logger, string cookieName);
    }
}
