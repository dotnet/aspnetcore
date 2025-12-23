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

    public CookieTempDataProvider(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
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
                convertedData[kvp.Key] = ConvertJsonElement(kvp.Value);
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
            if (!CanSerializeType(kvp.Value?.GetType() ?? typeof(object)))
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

    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                if (element.TryGetGuid(out var guid))
                {
                    return guid;
                }
                if (element.TryGetDateTime(out var dateTime))
                {
                    return dateTime;
                }
                return element.GetString();
            case JsonValueKind.Number:
                return element.GetInt32();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                return DeserializeArray(element);
            case JsonValueKind.Object:
                return DeserializeDictionaryEntry(element);
            default:
                throw new InvalidOperationException($"TempData cannot deserialize value of type '{element.ValueKind}'.");
        }
    }

    private static object? DeserializeArray(JsonElement arrayElement)
    {
        var arrayLength = arrayElement.GetArrayLength();
        if (arrayLength == 0)
        {
            return null;
        }
        if (arrayElement[0].ValueKind == JsonValueKind.String)
        {
            var array = new List<string?>(arrayLength);
            foreach (var item in arrayElement.EnumerateArray())
            {
                array.Add(item.GetString());
            }
            return array.ToArray();
        }
        else if (arrayElement[0].ValueKind == JsonValueKind.Number)
        {
            var array = new List<int>(arrayLength);
            foreach (var item in arrayElement.EnumerateArray())
            {
                array.Add(item.GetInt32());
            }
            return array.ToArray();
        }
        throw new InvalidOperationException($"TempData cannot deserialize array of type '{arrayElement[0].ValueKind}'.");
    }

    private static Dictionary<string, string?> DeserializeDictionaryEntry(JsonElement objectElement)
    {
        var dictionary = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var item in objectElement.EnumerateObject())
        {
            dictionary[item.Name] = item.Value.GetString();
        }
        return dictionary;
    }

    private static bool CanSerializeType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return
            type.IsEnum ||
            type == typeof(int) ||
            type == typeof(string) ||
            type == typeof(bool) ||
            type == typeof(DateTime) ||
            type == typeof(Guid) ||
            typeof(ICollection<int>).IsAssignableFrom(type) ||
            typeof(ICollection<string>).IsAssignableFrom(type) ||
            typeof(IDictionary<string, string>).IsAssignableFrom(type);
    }

    private static partial class Log
    {
        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);

        [LoggerMessage(3, LogLevel.Warning, "The temp data cookie {CookieName} could not be saved, because it is too large to fit in a single cookie.", EventName = "TempDataCookieSaveFailure")]
        public static partial void TempDataCookieSaveFailure(ILogger logger, string cookieName);
    }
}
