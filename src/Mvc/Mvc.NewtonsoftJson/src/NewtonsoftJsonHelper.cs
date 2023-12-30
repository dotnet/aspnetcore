// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// Newtonsoft.Json based implementation of <see cref="IJsonHelper"/>.
/// </summary>
internal sealed class NewtonsoftJsonHelper : IJsonHelper
{
    // Perf: JsonSerializers are relatively expensive to create, and are thread safe. Cache the serializer
    private readonly JsonSerializer _defaultSettingsJsonSerializer;
    private readonly IArrayPool<char> _charPool;

    /// <summary>
    /// Initializes a new instance of <see cref="NewtonsoftJsonHelper"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcNewtonsoftJsonOptions"/>.</param>
    /// <param name="charPool">
    /// The <see cref="ArrayPool{Char}"/> for use with custom <see cref="JsonSerializerSettings"/> (see
    /// <see cref="Serialize(object, JsonSerializerSettings)"/>).
    /// </param>
    public NewtonsoftJsonHelper(IOptions<MvcNewtonsoftJsonOptions> options, ArrayPool<char> charPool)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(charPool);

        _defaultSettingsJsonSerializer = CreateHtmlSafeSerializer(options.Value.SerializerSettings);
        _charPool = new JsonArrayPool<char>(charPool);
    }

    public IHtmlContent Serialize(object value)
    {
        return Serialize(value, _defaultSettingsJsonSerializer);
    }

    public IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings)
    {
        ArgumentNullException.ThrowIfNull(serializerSettings);

        var jsonSerializer = CreateHtmlSafeSerializer(serializerSettings);
        return Serialize(value, jsonSerializer);
    }

    private IHtmlContent Serialize(object value, JsonSerializer jsonSerializer)
    {
        using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
        {
            var jsonWriter = new JsonTextWriter(stringWriter)
            {
                ArrayPool = _charPool,
            };

            using (jsonWriter)
            {
                jsonSerializer.Serialize(jsonWriter, value);
            }

            return new HtmlString(stringWriter.ToString());
        }
    }

    private static JsonSerializer CreateHtmlSafeSerializer(JsonSerializerSettings serializerSettings)
    {
        var jsonSerializer = JsonSerializer.Create(serializerSettings);
        // Ignore the user configured StringEscapeHandling and always escape it.
        jsonSerializer.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
        return jsonSerializer;
    }
}
