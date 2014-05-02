// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class JsonInputFormatter : IInputFormatter
    {
        private const int DefaultMaxDepth = 32;
        private readonly List<Encoding> _supportedEncodings;
        private readonly List<string> _supportedMediaTypes;
        private JsonSerializerSettings _jsonSerializerSettings;

        public JsonInputFormatter()
        {
            _supportedMediaTypes = new List<string>
            {
                "application/json", 
                "text/json"
            };

            _supportedEncodings = new List<Encoding>
            {
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true)
            };

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
        public IList<string> SupportedMediaTypes
        {
            get { return _supportedMediaTypes; }
        }

        /// <inheritdoc />
        public IList<Encoding> SupportedEncodings
        {
            get { return _supportedEncodings; }
        }

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
                    throw new ArgumentNullException("value");
                }

                _jsonSerializerSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets if deserialization errors are captured. When set, these errors appear in 
        /// the <see cref="ModelStateDictionary"/> instance of <see cref="InputFormatterContext"/>.
        /// </summary>
        public bool CaptureDeserilizationErrors { get; set; }

        /// <inheritdoc />
        public async Task ReadAsync([NotNull] InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                var modelType = context.Metadata.ModelType;
                context.Model = modelType.GetTypeInfo().IsValueType ? Activator.CreateInstance(modelType) :
                                                                      null;
                return ;
            }

            // Get the character encoding for the content
            // Never non-null since SelectCharacterEncoding() throws in error / not found scenarios
            var effectiveEncoding = SelectCharacterEncoding(request.GetContentType());

            context.Model = await ReadInternal(context, effectiveEncoding);
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

        // <summary>
        /// Called during deserialization to get the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        public virtual JsonSerializer CreateJsonSerializer()
        {
            return JsonSerializer.Create(SerializerSettings);
        }

        private bool IsSupportedContentType(ContentTypeHeaderValue contentType)
        {
            return contentType != null &&
                   _supportedMediaTypes.Contains(contentType.ContentType, StringComparer.OrdinalIgnoreCase);
        }

        private Task<object> ReadInternal(InputFormatterContext context,
                                          Encoding effectiveEncoding)
        {
            var type = context.Metadata.ModelType;
            var request = context.HttpContext.Request;

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
                        context.ModelState.AddModelError(e.ErrorContext.Path, e.ErrorContext.Error);
                        // Error must always be marked as handled
                        // Failure to do so can cause the exception to be rethrown at every recursive level and overflow the
                        // stack for x64 CLR processes
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

        private Encoding SelectCharacterEncoding(ContentTypeHeaderValue contentType)
        {
            if (contentType != null)
            {
                // Find encoding based on content type charset parameter
                var charset = contentType.CharSet;
                if (!string.IsNullOrWhiteSpace(contentType.CharSet))
                {
                    for (var i = 0; i < _supportedEncodings.Count; i++)
                    {
                        var supportedEncoding = _supportedEncodings[i];
                        if (string.Equals(charset, supportedEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                        {
                            return supportedEncoding;
                        }
                    }
                }
            }

            if (_supportedEncodings.Count > 0)
            {
                return _supportedEncodings[0];
            }

            // No supported encoding was found so there is no way for us to start reading.
            throw new InvalidOperationException(Resources.FormatMediaTypeFormatterNoEncoding(GetType().FullName));
        }
    }
}
