
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal class SystemTextJsonHelper : IJsonHelper
    {
        private readonly JsonSerializerOptions _htmlSafeJsonSerializerOptions;

        public SystemTextJsonHelper(IOptions<JsonOptions> options)
        {
            _htmlSafeJsonSerializerOptions = CreateHtmlSafeSerializerOptions(options.Value.JsonSerializerOptions);
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            // JsonSerializer always encodes non-ASCII chars, so we do not need
            // to do anything special with the SerializerOptions
            var json = JsonSerializer.Serialize(value, _htmlSafeJsonSerializerOptions);
            return new HtmlString(json);
        }

        private static JsonSerializerOptions CreateHtmlSafeSerializerOptions(JsonSerializerOptions serializerOptions)
        {
            var htmlSafeOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = serializerOptions.AllowTrailingCommas,
                DefaultBufferSize = serializerOptions.DefaultBufferSize,
                DictionaryKeyPolicy = serializerOptions.DictionaryKeyPolicy,
                IgnoreNullValues = serializerOptions.IgnoreNullValues,
                IgnoreReadOnlyProperties = serializerOptions.IgnoreReadOnlyProperties,
                MaxDepth = serializerOptions.MaxDepth,
                PropertyNameCaseInsensitive = serializerOptions.PropertyNameCaseInsensitive,
                PropertyNamingPolicy = serializerOptions.PropertyNamingPolicy,
                ReadCommentHandling = serializerOptions.ReadCommentHandling,
                WriteIndented = serializerOptions.WriteIndented
            };

            // Use the HTML safe encoder
            htmlSafeOptions.Encoder = JavaScriptEncoder.Default;

            for (var i = 0; i < serializerOptions.Converters.Count; i++)
            {
                htmlSafeOptions.Converters.Add(serializerOptions.Converters[i]);
            }

            return htmlSafeOptions;
        }
    }
}
