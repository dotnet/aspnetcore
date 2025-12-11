// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class TempDataService
{
    private const string CookieName = ".AspNetCore.Components.TempData";

    public TempDataService()
    {
        // TO-DO: Add encoding later if needed
    }

    public static TempData Load(HttpContext httpContext)
    {
        var returnTempData = new TempData();
        var serializedDataFromCookie = httpContext.Request.Cookies[CookieName];
        if (serializedDataFromCookie is null)
        {
            return returnTempData;
        }

        var dataFromCookie = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serializedDataFromCookie);
        if (dataFromCookie is null)
        {
            return returnTempData;
        }

        var convertedData = new Dictionary<string, object?>();
        foreach (var kvp in dataFromCookie)
        {
            convertedData[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        returnTempData.LoadDataFromCookie(convertedData);
        return returnTempData;
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

    public static void Save(HttpContext httpContext, TempData tempData)
    {
        var dataToSave = tempData.GetDataToSave();
        foreach (var kvp in dataToSave)
        {
            if (!CanSerializeType(kvp.Value?.GetType() ?? typeof(object)))
            {
                throw new InvalidOperationException($"TempData cannot store values of type '{kvp.Value?.GetType()}'.");
            }
        }

        if (dataToSave.Count == 0)
        {
            httpContext.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = httpContext.Request.PathBase.HasValue ? httpContext.Request.PathBase.Value : "/",
            });
            return;
        }
        httpContext.Response.Cookies.Append(CookieName, JsonSerializer.Serialize(dataToSave), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Path = httpContext.Request.PathBase.HasValue ? httpContext.Request.PathBase.Value : "/",
        });
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
}
