// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
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
            if (jsonHelper == null)
            {
                throw new ArgumentNullException(nameof(jsonHelper));
            }

            if (!(jsonHelper is NewtonsoftJsonHelper newtonsoftJsonHelper))
            {
                var message = Resources.FormatJsonHelperMustBeAnInstanceOfNewtonsoftJson(
                    nameof(jsonHelper),
                    nameof(IJsonHelper),
                    typeof(JsonHelperExtensions).Assembly.GetName().Name,
                    nameof(NewtonsoftJsonMvcBuilderExtensions.AddNewtonsoftJson));

                throw new ArgumentException(message, nameof(jsonHelper));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            return newtonsoftJsonHelper.Serialize(value, serializerSettings);
        }
    }
}
