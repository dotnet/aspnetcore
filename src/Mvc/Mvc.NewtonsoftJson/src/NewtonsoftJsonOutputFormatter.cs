// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> for JSON content.
    /// </summary>
    public class NewtonsoftJsonOutputFormatter : TextOutputFormatter
    {
        private readonly IArrayPool<char> _charPool;
        private readonly MvcOptions _mvcOptions;

        // Perf: JsonSerializers are relatively expensive to create, and are thread safe. We cache
        // the serializer and invalidate it when the settings change.
        private JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new <see cref="NewtonsoftJsonOutputFormatter"/> instance.
        /// </summary>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcNewtonsoftJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="mvcOptions">The <see cref="MvcOptions"/>.</param>
        public NewtonsoftJsonOutputFormatter(
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            MvcOptions mvcOptions)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            SerializerSettings = serializerSettings;
            _charPool = new JsonArrayPool<char>(charPool);
            _mvcOptions = mvcOptions ?? throw new ArgumentNullException(nameof(mvcOptions));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <remarks>
        /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
        /// <see cref="NewtonsoftJsonOutputFormatter"/> has been used will have no effect.
        /// </remarks>
        protected JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> used to write.</param>
        /// <returns>The <see cref="JsonWriter"/> used during serialization.</returns>
        protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var jsonWriter = new JsonTextWriter(writer)
            {
                ArrayPool = _charPool,
                CloseOutput = false,
                AutoCompleteOnClose = false
            };

            return jsonWriter;
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonSerializer"/>.The formatter context
        /// that is passed gives an ability to create serializer specific to the context.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        protected virtual JsonSerializer CreateJsonSerializer()
        {
            if (_serializer == null)
            {
                _serializer = JsonSerializer.Create(SerializerSettings);
            }

            return _serializer;
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonSerializer"/>.The formatter context
        /// that is passed gives an ability to create serializer specific to the context.
        /// </summary>
        /// <param name="context">A context object for <see cref="IOutputFormatter.WriteAsync(OutputFormatterWriteContext)"/>.</param>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        protected virtual JsonSerializer CreateJsonSerializer(OutputFormatterWriteContext context)
        {
            return CreateJsonSerializer();
        }

        /// <inheritdoc />
        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var response = context.HttpContext.Response;

            var responseStream = response.Body;
            FileBufferingWriteStream fileBufferingWriteStream = null;
            if (!_mvcOptions.SuppressOutputFormatterBuffering)
            {
                fileBufferingWriteStream = new FileBufferingWriteStream();
                responseStream = fileBufferingWriteStream;
            }

            try
            {
                await using (var writer = context.WriterFactory(responseStream, selectedEncoding))
                {
                    using (var jsonWriter = CreateJsonWriter(writer))
                    {
                        var jsonSerializer = CreateJsonSerializer(context);
                        jsonSerializer.Serialize(jsonWriter, context.Object);
                    }
                }

                if (fileBufferingWriteStream != null)
                {
                    response.ContentLength = fileBufferingWriteStream.Length;
                    await fileBufferingWriteStream.DrainBufferAsync(response.Body);
                }
            }
            finally
            {
                if (fileBufferingWriteStream != null)
                {
                    await fileBufferingWriteStream.DisposeAsync();
                }
            }
        }
    }
}
