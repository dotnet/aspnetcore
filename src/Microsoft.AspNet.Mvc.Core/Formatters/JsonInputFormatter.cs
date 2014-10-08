// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonInputFormatter : IInputFormatter
    {
        private const int DefaultMaxDepth = 32;
        private JsonSerializerSettings _jsonSerializerSettings;

        public JsonInputFormatter()
        {
            SupportedEncodings = new List<Encoding>();
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);
            SupportedMediaTypes = new List<MediaTypeHeaderValue>();
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/json"));

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
                // from deserialization errors that might occur from deeply nested objects.
                MaxDepth = DefaultMaxDepth,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
                TypeNameHandling = TypeNameHandling.None
            };
        }

        /// <inheritdoc />
        public IList<MediaTypeHeaderValue> SupportedMediaTypes { get; private set; }

        /// <inheritdoc />
        public IList<Encoding> SupportedEncodings { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        public JsonSerializerSettings SerializerSettings
        {
            get { return _jsonSerializerSettings; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _jsonSerializerSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets if deserialization errors are captured. When set, these errors appear in 
        /// the <see cref="ActionContext.ModelState"/> instance of <see cref="InputFormatterContext"/>.
        /// </summary>
        public bool CaptureDeserilizationErrors { get; set; }

        /// <inheritdoc />
        public bool CanRead(InputFormatterContext context)
        {
            var contentType = context.ActionContext.HttpContext.Request.ContentType;
            MediaTypeHeaderValue requestContentType;
            if (!MediaTypeHeaderValue.TryParse(contentType, out requestContentType))
            {
                return false;
            }

            return SupportedMediaTypes
                            .Any(supportedMediaType => supportedMediaType.IsSubsetOf(requestContentType));
        }

        /// <inheritdoc />
        public async Task<object> ReadAsync([NotNull] InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                var modelType = context.ModelType;
                var model = modelType.GetTypeInfo().IsValueType ? Activator.CreateInstance(modelType) :
                                                                      null;
                return model;
            }

            MediaTypeHeaderValue requestContentType = null;
            MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType);

            // Get the character encoding for the content
            // Never non-null since SelectCharacterEncoding() throws in error / not found scenarios
            var effectiveEncoding = SelectCharacterEncoding(requestContentType);

            return await ReadInternal(context, effectiveEncoding);
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/> for the read.</param>
        /// <param name="readStream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="effectiveEncoding">The <see cref="Encoding"/> to use when reading.</param>
        /// <returns>The <see cref="JsonReader"/> used during deserialization.</returns>
        public virtual JsonReader CreateJsonReader([NotNull] InputFormatterContext context,
                                                   [NotNull] Stream readStream,
                                                   [NotNull] Encoding effectiveEncoding)
        {
            return new JsonTextReader(new StreamReader(readStream, effectiveEncoding));
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        public virtual JsonSerializer CreateJsonSerializer()
        {
            return JsonSerializer.Create(SerializerSettings);
        }

        private Task<object> ReadInternal(InputFormatterContext context,
                                          Encoding effectiveEncoding)
        {
            var type = context.ModelType;
            var request = context.ActionContext.HttpContext.Request;

            using (var jsonReader = CreateJsonReader(context, request.Body, effectiveEncoding))
            {
                jsonReader.CloseInput = false;

                var jsonSerializer = CreateJsonSerializer();

                EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> errorHandler = null;
                if (CaptureDeserilizationErrors)
                {
                    errorHandler = (sender, e) =>
                    {
                        var exception = e.ErrorContext.Error;
                        context.ActionContext.ModelState.TryAddModelError(e.ErrorContext.Path, e.ErrorContext.Error);
                        // Error must always be marked as handled
                        // Failure to do so can cause the exception to be rethrown at every recursive level and 
                        // overflow the stack for x64 CLR processes
                        e.ErrorContext.Handled = true;
                    };
                    jsonSerializer.Error += errorHandler;
                }

                try
                {
                    return Task.FromResult(jsonSerializer.Deserialize(jsonReader, type));
                }
                finally
                {
                    // Clean up the error handler in case CreateJsonSerializer() reuses a serializer
                    if (errorHandler != null)
                    {
                        jsonSerializer.Error -= errorHandler;
                    }
                }
            }
        }

        private Encoding SelectCharacterEncoding(MediaTypeHeaderValue contentType)
        {
            if (contentType != null)
            {
                // Find encoding based on content type charset parameter
                var charset = contentType.Charset;
                if (!string.IsNullOrWhiteSpace(contentType.Charset))
                {
                    foreach (var supportedEncoding in SupportedEncodings)
                    {
                        if (string.Equals(charset, supportedEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                        {
                            return supportedEncoding;
                        }
                    }
                }
            }

            if (SupportedEncodings.Count > 0)
            {
                return SupportedEncodings[0];
            }

            // No supported encoding was found so there is no way for us to start reading.
            throw new InvalidOperationException(Resources.FormatInputFormatterNoEncoding(GetType().FullName));
        }
    }
}
