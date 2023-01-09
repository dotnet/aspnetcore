// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextInputFormatter"/> for JSON content that uses <see cref="JsonSerializer"/>.
/// </summary>
public partial class SystemTextJsonInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
{
    private readonly JsonOptions _jsonOptions;
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
        _jsonOptions = options;
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        var httpContext = context.HttpContext;
        var (inputStream, usesTranscodingStream) = GetInputStream(httpContext, encoding);

        object? model;
        try
        {
            model = await JsonSerializer.DeserializeAsync(inputStream, context.ModelType, SerializerOptions);
        }
        catch (JsonException jsonException)
        {
            var path = jsonException.Path ?? string.Empty;

            var modelStateException = WrapExceptionForModelState(jsonException);

            context.ModelState.TryAddModelError(path, modelStateException, context.Metadata);

            Log.JsonInputException(_logger, jsonException);

            return InputFormatterResult.Failure();
        }
        catch (Exception exception) when (exception is FormatException || exception is OverflowException)
        {
            // The code in System.Text.Json never throws these exceptions. However a custom converter could produce these errors for instance when
            // parsing a value. These error messages are considered safe to report to users using ModelState.

            context.ModelState.TryAddModelError(string.Empty, exception, context.Metadata);
            Log.JsonInputException(_logger, exception);

            return InputFormatterResult.Failure();
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
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
            Log.JsonInputSuccess(_logger, context.ModelType);
            return InputFormatterResult.Success(model);
        }
    }

    private Exception WrapExceptionForModelState(JsonException jsonException)
    {
        if (!_jsonOptions.AllowInputFormatterExceptionMessages)
        {
            // This app is not opted-in to System.Text.Json messages, return the original exception.
            return jsonException;
        }

        // InputFormatterException specifies that the message is safe to return to a client, it will
        // be added to model state.
        return new InputFormatterException(jsonException.Message, jsonException);
    }

    private static (Stream inputStream, bool usesTranscodingStream) GetInputStream(HttpContext httpContext, Encoding encoding)
    {
        if (encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return (httpContext.Request.Body, false);
        }

        var inputStream = Encoding.CreateTranscodingStream(httpContext.Request.Body, encoding, Encoding.UTF8, leaveOpen: true);
        return (inputStream, true);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "JSON input formatter threw an exception: {Message}", EventName = "SystemTextJsonInputException")]
        private static partial void JsonInputException(ILogger logger, string message);

        public static void JsonInputException(ILogger logger, Exception exception)
            => JsonInputException(logger, exception.Message);

        [LoggerMessage(2, LogLevel.Debug, "JSON input formatter succeeded, deserializing to type '{TypeName}'", EventName = "SystemTextJsonInputSuccess")]
        private static partial void JsonInputSuccess(ILogger logger, string? typeName);

        public static void JsonInputSuccess(ILogger logger, Type modelType)
            => JsonInputSuccess(logger, modelType.FullName);
    }
}
