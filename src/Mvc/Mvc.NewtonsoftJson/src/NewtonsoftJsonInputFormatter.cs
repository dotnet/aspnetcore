// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextInputFormatter"/> for JSON content.
/// </summary>
public partial class NewtonsoftJsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
{
    private readonly IArrayPool<char> _charPool;
    private readonly ILogger _logger;
    private readonly ObjectPoolProvider _objectPoolProvider;
    private readonly MvcOptions _options;
    private readonly MvcNewtonsoftJsonOptions _jsonOptions;

    private ObjectPool<JsonSerializer>? _jsonSerializerPool;

    /// <summary>
    /// Initializes a new instance of <see cref="NewtonsoftJsonInputFormatter"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="serializerSettings">
    /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
    /// (<see cref="MvcNewtonsoftJsonOptions.SerializerSettings"/>) or an instance
    /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
    /// </param>
    /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
    /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    /// <param name="jsonOptions">The <see cref="MvcNewtonsoftJsonOptions"/>.</param>
    public NewtonsoftJsonInputFormatter(
        ILogger logger,
        JsonSerializerSettings serializerSettings,
        ArrayPool<char> charPool,
        ObjectPoolProvider objectPoolProvider,
        MvcOptions options,
        MvcNewtonsoftJsonOptions jsonOptions)
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
        _jsonOptions = jsonOptions;

        SupportedEncodings.Add(UTF8EncodingWithoutBOM);
        SupportedEncodings.Add(UTF16EncodingLittleEndian);

        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
    }

    /// <inheritdoc />
    public virtual InputFormatterExceptionPolicy ExceptionPolicy
    {
        get
        {
            if (GetType() == typeof(NewtonsoftJsonInputFormatter))
            {
                return InputFormatterExceptionPolicy.MalformedInputExceptions;
            }
            return InputFormatterExceptionPolicy.AllExceptions;
        }
    }

    /// <summary>
    /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
    /// <see cref="NewtonsoftJsonInputFormatter"/> has been used will have no effect.
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

        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        var suppressInputFormatterBuffering = _options.SuppressInputFormatterBuffering;

        var readStream = request.Body;
        var disposeReadStream = false;
        if (readStream.CanSeek)
        {
            // The most common way of getting here is the user has request buffering on.
            // However, request buffering isn't eager, and consequently it will peform pass-thru synchronous
            // reads as part of the deserialization.
            // To avoid this, drain and reset the stream.
            var position = request.Body.Position;
            await readStream.DrainAsync(CancellationToken.None);
            readStream.Position = position;
        }
        else if (!suppressInputFormatterBuffering)
        {
            // JSON.Net does synchronous reads. In order to avoid blocking on the stream, we asynchronously
            // read everything into a buffer, and then seek back to the beginning.
            var memoryThreshold = _jsonOptions.InputFormatterMemoryBufferThreshold;
            var contentLength = request.ContentLength.GetValueOrDefault();
            if (contentLength > 0 && contentLength < memoryThreshold)
            {
                // If the Content-Length is known and is smaller than the default buffer size, use it.
                memoryThreshold = (int)contentLength;
            }

            readStream = new FileBufferingReadStream(request.Body, memoryThreshold);
            // Ensure the file buffer stream is always disposed at the end of a request.
            httpContext.Response.RegisterForDispose(readStream);

            await readStream.DrainAsync(CancellationToken.None);
            readStream.Seek(0L, SeekOrigin.Begin);

            disposeReadStream = true;
        }

        var successful = true;
        Exception? exception = null;
        object? model;

        using (var streamReader = context.ReaderFactory(readStream, encoding))
        {
            using var jsonReader = new JsonTextReader(streamReader);
            jsonReader.ArrayPool = _charPool;
            jsonReader.CloseInput = false;

            var type = context.ModelType;
            var jsonSerializer = CreateJsonSerializer(context);
            jsonSerializer.Error += ErrorHandler;

            if (_jsonOptions.ReadJsonWithRequestCulture)
            {
                jsonSerializer.Culture = CultureInfo.CurrentCulture;
            }

            try
            {
                model = jsonSerializer.Deserialize(jsonReader, type);
            }
            finally
            {
                // Clean up the error handler since CreateJsonSerializer() pools instances.
                jsonSerializer.Error -= ErrorHandler;
                ReleaseJsonSerializer(jsonSerializer);

                if (disposeReadStream)
                {
                    await readStream.DisposeAsync();
                }
            }
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

        if (exception is not null && exception is not (JsonException or OverflowException or FormatException))
        {
            // At this point we've already recorded all exceptions as an entry in the ModelStateDictionary.
            // We only need to rethrow an exception if we believe it needs to be handled by something further up
            // the stack.
            // JsonException, OverflowException, and FormatException are assumed to be only encountered when
            // parsing the JSON and are consequently "safe" to be exposed as part of ModelState. Everything else
            // needs to be rethrown.

            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
            exceptionDispatchInfo.Throw();
        }

        return InputFormatterResult.Failure();

        void ErrorHandler(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs eventArgs)
        {
            // Skipping error, if it's already marked as handled
            // This allows user code to implement its own error handling
            if (eventArgs.ErrorContext.Handled)
            {
                return;
            }

            successful = false;

            // The following addMember logic is intended to append the names of missing required properties to the
            // ModelStateDictionary key. Normally, just the ModelName and ErrorContext.Path is used for this key,
            // but ErrorContext.Path does not include the missing required property name like we want it to.
            // For example, given the following class and input missing the required "Name" property:
            //
            // class Person
            // {
            //     [JsonProperty(Required = Required.Always)]
            //     public string Name { get; set; }
            // }
            //
            // We will see the following ErrorContext:
            //
            // Error   {"Required property 'Name' not found in JSON. Path 'Person'..."} System.Exception {Newtonsoft.Json.JsonSerializationException}
            // Member  "Name"   object {string}
            // Path    "Person" string
            //
            // So we update the path used for the ModelStateDictionary key to be "Person.Name" instead of just "Person".
            // See https://github.com/aspnet/Mvc/issues/8509
            var path = eventArgs.ErrorContext.Path;
            var member = eventArgs.ErrorContext.Member as string;

            // There are some deserialization exceptions that include the member in the path but not at the end.
            // For example, given the following classes and invalid input like { "b": { "c": { "d": abc } } }:
            //
            // class A
            // {
            //     public B B { get; set; }
            // }
            // class B
            // {
            //     public C C { get; set; }
            // }
            // class C
            // {
            //     public string D { get; set; }
            // }
            //
            // We will see the following ErrorContext:
            //
            // Error   {"Unexpected character encountered while parsing value: b. Path 'b.c.d'..."} System.Exception {Newtonsoft.Json.JsonReaderException}
            // Member  "c"     object {string}
            // Path    "b.c.d" string
            //
            // Notice that Member "c" is in the middle of the Path "b.c.d". The error handler gets invoked for each level of nesting.
            // null, "b", "c" and "d" are each a Member in different ErrorContexts all reporting the same parsing error.
            //
            // The parsing error is reported as a JsonReaderException instead of as a JsonSerializationException like
            // for missing required properties. We use the exception type to filter out these errors and keep the path used
            // for the ModelStateDictionary key as "b.c.d" instead of "b.c.d.c"
            // See https://github.com/dotnet/aspnetcore/issues/33451
            var addMember = !string.IsNullOrEmpty(member) && eventArgs.ErrorContext.Error is JsonSerializationException;

            // There are still JsonSerilizationExceptions that set ErrorContext.Member but include it at the
            // end of ErrorContext.Path already. The following logic attempts to filter these out.
            if (addMember)
            {
                // Path.Member case (path.Length < member.Length) needs no further checks.
                if (path.Length == member!.Length)
                {
                    // Add Member in Path.Member case but not for Path.Path.
                    addMember = !string.Equals(path, member, StringComparison.Ordinal);
                }
                else if (path.Length > member.Length)
                {
                    // Finally, check whether Path already ends or starts with Member.
                    if (member[0] == '[')
                    {
                        addMember = !path.EndsWith(member, StringComparison.Ordinal);
                    }
                    else
                    {
                        addMember = !path.EndsWith($".{member}", StringComparison.Ordinal)
                            && !path.EndsWith($"['{member}']", StringComparison.Ordinal)
                            && !path.EndsWith($"[{member}]", StringComparison.Ordinal);
                    }
                }
            }

            if (addMember)
            {
                path = ModelNames.CreatePropertyModelName(path, member);
            }

            // Handle path combinations such as ""+"Property", "Parent"+"Property", or "Parent"+"[12]".
            var key = ModelNames.CreatePropertyModelName(context.ModelName, path);

            exception = eventArgs.ErrorContext.Error;

            var metadata = GetPathMetadata(context.Metadata, path);
            var modelStateException = WrapExceptionForModelState(exception);
            context.ModelState.TryAddModelError(key, modelStateException, metadata);
            Log.JsonInputException(_logger, exception);

            // Error must always be marked as handled
            // Failure to do so can cause the exception to be rethrown at every recursive level and
            // overflow the stack for x64 CLR processes
            eventArgs.ErrorContext.Handled = true;
        }
    }

    /// <summary>
    /// Called during deserialization to get the <see cref="JsonSerializer"/>. The formatter context
    /// that is passed gives an ability to create serializer specific to the context.
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
    /// Called during deserialization to get the <see cref="JsonSerializer"/>. The formatter context
    /// that is passed gives an ability to create serializer specific to the context.
    /// </summary>
    /// <param name="context">A context object used by an input formatter for deserializing the request body into an object.</param>
    /// <returns>The <see cref="JsonSerializer"/> used during deserialization.</returns>
    /// <remarks>
    /// This method works in tandem with <see cref="ReleaseJsonSerializer(JsonSerializer)"/> to
    /// manage the lifetimes of <see cref="JsonSerializer"/> instances.
    /// </remarks>
    protected virtual JsonSerializer CreateJsonSerializer(InputFormatterContext context)
    {
        return CreateJsonSerializer();
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
        => _jsonSerializerPool!.Return(serializer);

    private static ModelMetadata GetPathMetadata(ModelMetadata metadata, string path)
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
                var propertyMetadata = metadata.Properties[propertyName];
                if (propertyMetadata is null)
                {
                    // Odd case but don't throw just because ErrorContext had an odd-looking path.
                    break;
                }
                metadata = propertyMetadata;
                index = endIndex;
            }
        }

        return metadata;
    }

    private Exception WrapExceptionForModelState(Exception exception)
    {
        // In 2.0 and earlier we always gave a generic error message for errors that come from JSON.NET
        // We only allow it in 2.1 and newer if the app opts-in.
        if (!_jsonOptions.AllowInputFormatterExceptionMessages)
        {
            // This app is not opted-in to JSON.NET messages, return the original exception.
            return exception;
        }

        // It's not known that Json.NET currently ever raises error events with exceptions
        // other than these two types, but we're being conservative and limiting which ones
        // we regard as having safe messages to expose to clients
        if (exception is JsonReaderException || exception is JsonSerializationException)
        {
            // InputFormatterException specifies that the message is safe to return to a client, it will
            // be added to model state.
            return new InputFormatterException(exception.Message, exception);
        }

        // Not a known exception type, so we're not going to assume that it's safe.
        return exception;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "JSON input formatter threw an exception.", EventName = "JsonInputException")]
        public static partial void JsonInputException(ILogger logger, Exception exception);
    }
}
