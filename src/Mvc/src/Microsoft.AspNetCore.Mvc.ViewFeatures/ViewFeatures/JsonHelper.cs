// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Default implementation of <see cref="IJsonHelper"/>.
    /// </summary>
    public class JsonHelper : IJsonHelper
    {
        private readonly JsonOutputFormatter _jsonOutputFormatter;
        private readonly ArrayPool<char> _charPool;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonHelper"/> that is backed by <paramref name="jsonOutputFormatter"/>.
        /// </summary>
        /// <param name="jsonOutputFormatter">The <see cref="JsonOutputFormatter"/> used to serialize JSON.</param>
        /// <param name="charPool">
        /// The <see cref="ArrayPool{Char}"/> for use with custom <see cref="JsonSerializerSettings"/> (see
        /// <see cref="Serialize(object, JsonSerializerSettings)"/>).
        /// </param>
        public JsonHelper(JsonOutputFormatter jsonOutputFormatter, ArrayPool<char> charPool)
        {
            if (jsonOutputFormatter == null)
            {
                throw new ArgumentNullException(nameof(jsonOutputFormatter));
            }
            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            _jsonOutputFormatter = jsonOutputFormatter;
            _charPool = charPool;
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            var settings = ShallowCopy(_jsonOutputFormatter.PublicSerializerSettings);
            settings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

            return Serialize(value, settings);
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            var jsonOutputFormatter = new JsonOutputFormatter(serializerSettings, _charPool);

            return SerializeInternal(jsonOutputFormatter, value);
        }

        private IHtmlContent SerializeInternal(JsonOutputFormatter jsonOutputFormatter, object value)
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            jsonOutputFormatter.WriteObject(stringWriter, value);

            return new HtmlString(stringWriter.ToString());
        }

        private static JsonSerializerSettings ShallowCopy(JsonSerializerSettings settings)
        {
            var copiedSettings = new JsonSerializerSettings
            {
                FloatParseHandling = settings.FloatParseHandling,
                FloatFormatHandling = settings.FloatFormatHandling,
                DateParseHandling = settings.DateParseHandling,
                DateTimeZoneHandling = settings.DateTimeZoneHandling,
                DateFormatHandling = settings.DateFormatHandling,
                Formatting = settings.Formatting,
                MaxDepth = settings.MaxDepth,
                DateFormatString = settings.DateFormatString,
                Context = settings.Context,
                Error = settings.Error,
                SerializationBinder = settings.SerializationBinder,
                TraceWriter = settings.TraceWriter,
                Culture = settings.Culture,
                ReferenceResolverProvider = settings.ReferenceResolverProvider,
                EqualityComparer = settings.EqualityComparer,
                ContractResolver = settings.ContractResolver,
                ConstructorHandling = settings.ConstructorHandling,
                TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling,
                MetadataPropertyHandling = settings.MetadataPropertyHandling,
                TypeNameHandling = settings.TypeNameHandling,
                PreserveReferencesHandling = settings.PreserveReferencesHandling,
                Converters = settings.Converters,
                DefaultValueHandling = settings.DefaultValueHandling,
                NullValueHandling = settings.NullValueHandling,
                ObjectCreationHandling = settings.ObjectCreationHandling,
                MissingMemberHandling = settings.MissingMemberHandling,
                ReferenceLoopHandling = settings.ReferenceLoopHandling,
                CheckAdditionalContent = settings.CheckAdditionalContent,
            };

            return copiedSettings;
        }
    }
}
