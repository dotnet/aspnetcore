// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonOutputFormatter : OutputFormatter
    {
        private readonly JsonSerializerSettings _settings;
        private readonly bool _indent;

        public JsonOutputFormatter([NotNull] JsonSerializerSettings settings, bool indent)
        {
            _settings = settings;
            _indent = indent;
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/json"));
        }

        public static JsonSerializerSettings CreateDefaultSettings()
        {
            return new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types.
                TypeNameHandling = TypeNameHandling.None
            };
        }

        public void WriteObject([NotNull] TextWriter writer, object value)
        {
            using (var jsonWriter = CreateJsonWriter(writer))
            {
                var jsonSerializer = CreateJsonSerializer();
                jsonSerializer.Serialize(jsonWriter, value);

                // We're explicitly calling flush here to simplify the debugging experience because the
                // underlying TextWriter might be long-lived. If this method ends up being called repeatedly
                // for a request, we should revisit.
                jsonWriter.Flush();
            }
        }

        private JsonWriter CreateJsonWriter(TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            if (_indent)
            {
                jsonWriter.Formatting = Formatting.Indented;
            }

            jsonWriter.CloseOutput = false;

            return jsonWriter;
        }

        private JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = JsonSerializer.Create(_settings);
            return jsonSerializer;
        }

        public override bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            return SupportedMediaTypes.Any(supportedMediaType => 
                                            contentType.RawValue.Equals(supportedMediaType.RawValue,
                                                                        StringComparison.OrdinalIgnoreCase));
        }

        public override Task WriteAsync(OutputFormatterContext context,
                                        CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = context.HttpContext.Response;

            // The content type including the encoding should have been set already. 
            // In case it was not present, a default will be selected. 
            var selectedEncoding = SelectCharacterEncoding(MediaTypeHeaderValue.Parse(response.ContentType));
            using (var writer = new StreamWriter(response.Body, selectedEncoding))
            {
                using (var jsonWriter = CreateJsonWriter(writer))
                {
                    var jsonSerializer = CreateJsonSerializer();
                    jsonSerializer.Serialize(jsonWriter, context.ObjectResult.Value);

                    // We're explicitly calling flush here to simplify the debugging experience because the
                    // underlying TextWriter might be long-lived. If this method ends up being called repeatedly
                    // for a request, we should revisit.
                    jsonWriter.Flush();
                }
            }

            return Task.FromResult(true);
        }
    }
}
