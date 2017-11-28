// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextInputFormatter"/> for JSON content.
    /// </summary>
    public class JsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
    {
        private readonly IArrayPool<char> _charPool;
        private readonly ILogger _logger;
        private readonly ObjectPoolProvider _objectPoolProvider;
        private readonly MvcOptions _options;
        private readonly bool _suppressInputFormatterBuffering;
        private readonly bool _suppressJsonDeserializationExceptionMessages;

        private ObjectPool<JsonSerializer> _jsonSerializerPool;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonInputFormatter"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider) :
            this(logger, serializerSettings, charPool, objectPoolProvider, suppressInputFormatterBuffering: false)
        {
            // This constructor by default buffers the request body as its the most secure setting 
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonInputFormatter"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="suppressInputFormatterBuffering">Flag to buffer entire request body before deserializing it.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            bool suppressInputFormatterBuffering)
            : this(logger, serializerSettings, charPool, objectPoolProvider, suppressInputFormatterBuffering, suppressJsonDeserializationExceptionMessages: false)
        {
            // This constructor by default treats JSON deserialization exceptions as safe
            // because this is the default for applications generally
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonInputFormatter"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="suppressInputFormatterBuffering">Flag to buffer entire request body before deserializing it.</param>
        /// <param name="suppressJsonDeserializationExceptionMessages">If <see langword="true"/>, JSON deserialization exception messages will replaced by a generic message in model state.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            bool suppressInputFormatterBuffering,
            bool suppressJsonDeserializationExceptionMessages)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (objectPoolProvider == null)
            {
                throw new ArgumentNullException(nameof(objectPoolProvider));
            }

            _logger = logger;
            SerializerSettings = serializerSettings;
            _charPool = new JsonArrayPool<char>(charPool);
            _objectPoolProvider = objectPoolProvider;
            _suppressInputFormatterBuffering = suppressInputFormatterBuffering;
            _suppressJsonDeserializationExceptionMessages = suppressJsonDeserializationExceptionMessages;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonInputFormatter"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public JsonInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            MvcOptions options)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (objectPoolProvider == null)
            {
                throw new ArgumentNullException(nameof(objectPoolProvider));
            }

            _logger = logger;
            SerializerSettings = serializerSettings;
            _charPool = new JsonArrayPool<char>(charPool);
            _objectPoolProvider = objectPoolProvider;
            _options = options;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <inheritdoc />
        public virtual InputFormatterExceptionModelStatePolicy ExceptionPolicy
        {
            get
            {
                if (GetType() == typeof(JsonInputFormatter))
                {
                    return InputFormatterExceptionModelStatePolicy.MalformedInputExceptions;
                }
                return InputFormatterExceptionModelStatePolicy.AllExceptions;
            }
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <remarks>
        /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
        /// <see cref="JsonInputFormatter"/> has been used will have no effect.
        /// </remarks>
        protected JsonSerializerSettings SerializerSettings { get; }

        /// <inheritdoc />
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
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

            var request = context.HttpContext.Request;

            var suppressInputFormatterBuffering = _options?.SuppressInputFormatterBuffering ?? _suppressInputFormatterBuffering;

            if (!request.Body.CanSeek && !suppressInputFormatterBuffering)
            {
                // JSON.Net does synchronous reads. In order to avoid blocking on the stream, we asynchronously 
                // read everything into a buffer, and then seek back to the beginning. 
                BufferingHelper.EnableRewind(request);
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(CancellationToken.None);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            using (var streamReader = context.ReaderFactory(request.Body, encoding))
            {
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    jsonReader.ArrayPool = _charPool;
                    jsonReader.CloseInput = false;

                    var successful = true;
                    Exception exception = null;
                    void ErrorHandler(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs eventArgs)
                    {
                        successful = false;

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

                        var metadata = GetPathMetadata(context.Metadata, eventArgs.ErrorContext.Path);
                        var modelStateException = WrapExceptionForModelState(eventArgs.ErrorContext.Error);
                        context.ModelState.TryAddModelError(key, modelStateException, metadata);

                        _logger.JsonInputException(eventArgs.ErrorContext.Error);

                        exception = eventArgs.ErrorContext.Error;

                        // Error must always be marked as handled
                        // Failure to do so can cause the exception to be rethrown at every recursive level and
                        // overflow the stack for x64 CLR processes
                        eventArgs.ErrorContext.Handled = true;
                    }

                    var type = context.ModelType;
                    var jsonSerializer = CreateJsonSerializer();
                    jsonSerializer.Error += ErrorHandler;
                    object model;
                    try
                    {
                        model = jsonSerializer.Deserialize(jsonReader, type);
                    }
                    finally
                    {
                        // Clean up the error handler since CreateJsonSerializer() pools instances.
                        jsonSerializer.Error -= ErrorHandler;
                        ReleaseJsonSerializer(jsonSerializer);
                    }

                    if (successful)
                    {
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

                    if (!(exception is JsonException || exception is OverflowException))
                    {
                        var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                        exceptionDispatchInfo.Throw();
                    }

                    return InputFormatterResult.Failure();
                }
            }
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during deserialization.</returns>
        /// <remarks>
        /// This method works in tandem with <see cref="ReleaseJsonSerializer(JsonSerializer)"/> to
        /// manage the lifetimes of <see cref="JsonSerializer"/> instances.
        /// </remarks>
        protected virtual JsonSerializer CreateJsonSerializer()
        {
            if (_jsonSerializerPool == null)
            {
                _jsonSerializerPool = _objectPoolProvider.Create(new JsonSerializerObjectPolicy(SerializerSettings));
            }

            return _jsonSerializerPool.Get();
        }

        /// <summary>
        /// Releases the <paramref name="serializer"/> instance.
        /// </summary>
        /// <param name="serializer">The <see cref="JsonSerializer"/> to release.</param>
        /// <remarks>
        /// This method works in tandem with <see cref="ReleaseJsonSerializer(JsonSerializer)"/> to
        /// manage the lifetimes of <see cref="JsonSerializer"/> instances.
        /// </remarks>
        protected virtual void ReleaseJsonSerializer(JsonSerializer serializer)
            => _jsonSerializerPool.Return(serializer);

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

        private Exception WrapExceptionForModelState(Exception exception)
        {
            // It's not known that Json.NET currently ever raises error events with exceptions
            // other than these two types, but we're being conservative and limiting which ones
            // we regard as having safe messages to expose to clients
            var isJsonExceptionType =
                exception is JsonReaderException || exception is JsonSerializationException;
            var suppressJsonDeserializationExceptionMessages = _options?.SuppressJsonDeserializationExceptionMessagesInModelState ?? _suppressJsonDeserializationExceptionMessages;
            var suppressOriginalMessage =
                suppressJsonDeserializationExceptionMessages || !isJsonExceptionType;
            return suppressOriginalMessage
                ? exception
                : new InputFormatterException(exception.Message, exception);
        }
    }
}
