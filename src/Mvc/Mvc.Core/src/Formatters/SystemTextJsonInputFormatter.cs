// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextInputFormatter"/> for JSON content that uses <see cref="JsonSerializer"/>.
    /// </summary>
    public class SystemTextJsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
    {
        private readonly ILogger<SystemTextJsonInputFormatter> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="SystemTextJsonInputFormatter"/>.
        /// </summary>
        /// <param name="options">The <see cref="JsonOptions"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public SystemTextJsonInputFormatter(
            JsonOptions options,
            ILogger<SystemTextJsonInputFormatter> logger)
        {
            SerializerOptions = options.JsonSerializerOptions;
            _logger = logger;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <remarks>
        /// A single instance of <see cref="SystemTextJsonInputFormatter"/> is used for all JSON formatting. Any
        /// changes to the options will affect all input formatting.
        /// </remarks>
        public JsonSerializerOptions SerializerOptions { get; }

        /// <inheritdoc />
        InputFormatterExceptionPolicy IInputFormatterExceptionPolicy.ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;

        /// <inheritdoc />
        public sealed override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var httpContext = context.HttpContext;
            var inputStream = GetInputStream(httpContext, encoding);

            object model;
            try
            {
                model = await JsonSerializer.ReadAsync(inputStream, context.ModelType, SerializerOptions);
            }
            catch (JsonException jsonException)
            {
                var path = jsonException.Path;
                if (path.StartsWith("$.", StringComparison.Ordinal))
                {
                    path = path.Substring(2);
                }

                // Handle path combinations such as ""+"Property", "Parent"+"Property", or "Parent"+"[12]".
                var key = ModelNames.CreatePropertyModelName(context.ModelName, path);

                var formatterException = new InputFormatterException(jsonException.Message, jsonException);

                var metadata = GetPathMetadata(context.Metadata, path);
                context.ModelState.TryAddModelError(key, formatterException, metadata);

                Log.JsonInputException(_logger, jsonException);

                return InputFormatterResult.Failure();
            }
            finally
            {
                if (inputStream is TranscodingReadStream transcoding)
                {
                    await transcoding.DisposeAsync();
                }
            }

            if (model == null && !context.TreatEmptyInputAsDefaultValue)
            {
                // Some nonempty inputs might deserialize as null, for example whitespace,
                // or the JSON-encoded value "null". The upstream BodyModelBinder needs to
                // be notified that we don't regard this as a real input so it can register
                // a model binding error.
                return InputFormatterResult.NoValue();
            }
            else
            {
                return InputFormatterResult.Success(model);
            }
        }

        private Stream GetInputStream(HttpContext httpContext, Encoding encoding)
        {
            if (encoding.CodePage == Encoding.UTF8.CodePage)
            {
                return httpContext.Request.Body;
            }

            return new TranscodingReadStream(httpContext.Request.Body, encoding);
        }

        // Keep in sync with NewtonsoftJsonInputFormatter.GetPatMetadata
        private ModelMetadata GetPathMetadata(ModelMetadata metadata, string path)
        {
            var index = 0;
            while (index >= 0 && index < path.Length)
            {
                if (path[index] == '[')
                {
                    // At start of "[0]".
                    if (metadata.ElementMetadata == null)
                    {
                        // Odd case but don't throw just because ErrorContext had an odd-looking path.
                        break;
                    }

                    metadata = metadata.ElementMetadata;
                    index = path.IndexOf(']', index);
                }
                else if (path[index] == '.' || path[index] == ']')
                {
                    // Skip '.' in "prefix.property" or "[0].property" or ']' in "[0]".
                    index++;
                }
                else
                {
                    // At start of "property", "property." or "property[0]".
                    var endIndex = path.IndexOfAny(new[] { '.', '[' }, index);
                    if (endIndex == -1)
                    {
                        endIndex = path.Length;
                    }

                    var propertyName = path.Substring(index, endIndex - index);
                    if (metadata.Properties[propertyName] == null)
                    {
                        // Odd case but don't throw just because ErrorContext had an odd-looking path.
                        break;
                    }

                    metadata = metadata.Properties[propertyName];
                    index = endIndex;
                }
            }

            return metadata;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _jsonInputFormatterException;

            static Log()
            {
                _jsonInputFormatterException = LoggerMessage.Define(
                   LogLevel.Debug,
                   new EventId(1, "SystemTextJsonInputException"),
                   "JSON input formatter threw an exception.");
            }

            public static void JsonInputException(ILogger logger, Exception exception) 
                => _jsonInputFormatterException(logger, exception);
        }
    }
}
