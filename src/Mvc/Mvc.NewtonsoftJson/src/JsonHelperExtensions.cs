// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Newtonsoft.Json specific extensions to <see cref="IJsonHelper"/>.
/// </summary>
public static class JsonHelperExtensions
{
    /// <summary>
    /// Returns serialized JSON for the <paramref name="value"/>.
    /// </summary>
    /// <param name="jsonHelper">The <see cref="IJsonHelper"/>.</param>
    /// <param name="value">The value to serialize as JSON.</param>
    /// <param name="serializerSettings">
    /// The <see cref="JsonSerializerSettings"/> to be used by the serializer.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the serialized JSON.</returns>
    /// <remarks>
    /// The value for <see cref="JsonSerializerSettings.StringEscapeHandling" /> from <paramref name="serializerSettings"/>
    /// is ignored by this method and <see cref="StringEscapeHandling.EscapeHtml"/> is always used.
    /// </remarks>
    public static IHtmlContent Serialize(
        this IJsonHelper jsonHelper,
        object value,
        JsonSerializerSettings serializerSettings)
    {
        ArgumentNullException.ThrowIfNull(jsonHelper);

        if (!(jsonHelper is NewtonsoftJsonHelper newtonsoftJsonHelper))
        {
            var message = Resources.FormatJsonHelperMustBeAnInstanceOfNewtonsoftJson(
                nameof(jsonHelper),
                nameof(IJsonHelper),
                typeof(JsonHelperExtensions).Assembly.GetName().Name,
                nameof(NewtonsoftJsonMvcBuilderExtensions.AddNewtonsoftJson));

            throw new ArgumentException(message, nameof(jsonHelper));
        }

        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(serializerSettings);

        return newtonsoftJsonHelper.Serialize(value, serializerSettings);
    }
}
