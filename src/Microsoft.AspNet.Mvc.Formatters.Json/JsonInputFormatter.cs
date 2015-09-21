// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class JsonInputFormatter : InputFormatter
    {
        private JsonSerializerSettings _serializerSettings;

        public JsonInputFormatter()
            : this(SerializerSettingsProvider.CreateSerializerSettings())
        {
        }

        public JsonInputFormatter(JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            _serializerSettings = serializerSettings;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
        }

        /// <summary>
        /// Gets or sets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        public JsonSerializerSettings SerializerSettings
        {
            get
            {
                return _serializerSettings;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _serializerSettings = value;
            }
        }

        /// <inheritdoc />
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Get the character encoding for the content.
            var effectiveEncoding = SelectCharacterEncoding(context);
            if (effectiveEncoding == null)
            {
                return InputFormatterResult.FailureAsync();
            }

            var request = context.HttpContext.Request;
            using (var jsonReader = CreateJsonReader(context, request.Body, effectiveEncoding))
            {
                jsonReader.CloseInput = false;

                var successful = true;
                EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> errorHandler = (sender, eventArgs) =>
                {
                    successful = false;

                    var exception = eventArgs.ErrorContext.Error;

                    // Handle path combinations such as "" + "Property", "Parent" + "Property", or "Parent" + "[12]".
                    var key = eventArgs.ErrorContext.Path;
                    if (!string.IsNullOrEmpty(context.ModelName))
                    {
                        if (string.IsNullOrEmpty(eventArgs.ErrorContext.Path))
                        {
                            key = context.ModelName;
                        }
                        else if (eventArgs.ErrorContext.Path[0] == '[')
                        {
                            key = context.ModelName + eventArgs.ErrorContext.Path;
                        }
                        else
                        {
                            key = context.ModelName + "." + eventArgs.ErrorContext.Path;
                        }
                    }

                    context.ModelState.TryAddModelError(key, eventArgs.ErrorContext.Error);

                    // Error must always be marked as handled
                    // Failure to do so can cause the exception to be rethrown at every recursive level and
                    // overflow the stack for x64 CLR processes
                    eventArgs.ErrorContext.Handled = true;
                };

                var type = context.ModelType;
                var jsonSerializer = CreateJsonSerializer();
                jsonSerializer.Error += errorHandler;

                object model;
                try
                {
                    model = jsonSerializer.Deserialize(jsonReader, type);
                }
                finally
                {
                    // Clean up the error handler in case CreateJsonSerializer() reuses a serializer
                    jsonSerializer.Error -= errorHandler;
                }

                if (successful)
                {
                    return InputFormatterResult.SuccessAsync(model);
                }

                return InputFormatterResult.FailureAsync();
            }
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/> for the read.</param>
        /// <param name="readStream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
        /// <returns>The <see cref="JsonReader"/> used during deserialization.</returns>
        protected virtual JsonReader CreateJsonReader(
            InputFormatterContext context,
            Stream readStream,
            Encoding effectiveEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (readStream == null)
            {
                throw new ArgumentNullException(nameof(readStream));
            }

            if (effectiveEncoding == null)
            {
                throw new ArgumentNullException(nameof(effectiveEncoding));
            }

            return new JsonTextReader(new StreamReader(readStream, effectiveEncoding));
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        protected virtual JsonSerializer CreateJsonSerializer()
        {
            return JsonSerializer.Create(SerializerSettings);
        }
    }
}
