// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CookieTempDataProvider : ITempDataProvider
{
    private const string CookieName = ".AspNetCore.Components.TempData";
    private const string Purpose = "Microsoft.AspNetCore.Components.CookieTempDataProviderToken.v1";
    private const int MaxEncodedLength = 4050;
    private readonly IDataProtector _dataProtector;
    private readonly ITempDataSerializer _tempDataSerializer;

    public CookieTempDataProvider(
        IDataProtectionProvider dataProtectionProvider,
        ITempDataSerializer tempDataSerializer)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _tempDataSerializer = tempDataSerializer;
    }

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        try
        {
            var serializedDataFromCookie = context.Request.Cookies[CookieName];
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
                convertedData[kvp.Key] = _tempDataSerializer.ConvertJsonElement(kvp.Value);
            }
            return convertedData;
        }
        catch (Exception ex)
        {
            // If any error occurs during loading (e.g. data protection key changed, malformed cookie),
            // return an empty TempData dictionary.
            if (context.RequestServices.GetService<ILogger<CookieTempDataProvider>>() is { } logger)
            {
                Log.TempDataCookieLoadFailure(logger, CookieName, ex);
            }

            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
            });
            return new Dictionary<string, object?>();
        }
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
        foreach (var kvp in values)
        {
            if (!_tempDataSerializer.CanSerializeType(kvp.Value?.GetType() ?? typeof(object)))
            {
                throw new InvalidOperationException($"TempData cannot store values of type '{kvp.Value?.GetType()}'.");
            }
        }

        if (values.Count == 0)
        {
            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
            });
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(values);
        var protectedBytes = _dataProtector.Protect(bytes);
        var encodedValue = WebEncoders.Base64UrlEncode(protectedBytes);

        if (encodedValue.Length > MaxEncodedLength)
        {
            if (context.RequestServices.GetService<ILogger<CookieTempDataProvider>>() is { } logger)
            {
                Log.TempDataCookieSaveFailure(logger, CookieName);
            }

            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
            });
            return;
        }

        context.Response.Cookies.Append(CookieName, encodedValue, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/",
        });
    }

    private static partial class Log
    {
        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);

        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be saved, because it is too large to fit in a single cookie.", EventName = "TempDataCookieSaveFailure")]
        public static partial void TempDataCookieSaveFailure(ILogger logger, string cookieName);
    }
}
