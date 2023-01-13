// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// Executes a <see cref="JsonResult"/> to write to the response.
/// </summary>
internal sealed partial class NewtonsoftJsonResultExecutor : IActionResultExecutor<JsonResult>
{
    private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json")
    {
        Encoding = Encoding.UTF8
    }.ToString();

    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly ILogger _logger;
    private readonly MvcOptions _mvcOptions;
    private readonly MvcNewtonsoftJsonOptions _jsonOptions;
    private readonly IArrayPool<char> _charPool;
    private readonly AsyncEnumerableReader _asyncEnumerableReaderFactory;

    /// <summary>
    /// Creates a new <see cref="NewtonsoftJsonResultExecutor"/>.
    /// </summary>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    /// <param name="logger">The <see cref="ILogger{NewtonsoftJsonResultExecutor}"/>.</param>
    /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
    /// <param name="jsonOptions">Accessor to <see cref="MvcNewtonsoftJsonOptions"/>.</param>
    /// <param name="charPool">The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.</param>
    public NewtonsoftJsonResultExecutor(
        IHttpResponseStreamWriterFactory writerFactory,
        ILogger<NewtonsoftJsonResultExecutor> logger,
        IOptions<MvcOptions> mvcOptions,
        IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
        ArrayPool<char> charPool)
    {
        ArgumentNullException.ThrowIfNull(writerFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(jsonOptions);
        ArgumentNullException.ThrowIfNull(charPool);

        _writerFactory = writerFactory;
        _logger = logger;
        _mvcOptions = mvcOptions?.Value ?? throw new ArgumentNullException(nameof(mvcOptions));
        _jsonOptions = jsonOptions.Value;
        _charPool = new JsonArrayPool<char>(charPool);
        _asyncEnumerableReaderFactory = new AsyncEnumerableReader(_mvcOptions);
    }

    /// <summary>
    /// Executes the <see cref="JsonResult"/> and writes the response.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="result">The <see cref="JsonResult"/>.</param>
    /// <returns>A <see cref="Task"/> which will complete when writing has completed.</returns>
    public async Task ExecuteAsync(ActionContext context, JsonResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var jsonSerializerSettings = GetSerializerSettings(result);

        var response = context.HttpContext.Response;

        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            result.ContentType,
            response.ContentType,
            (DefaultContentType, Encoding.UTF8),
            MediaType.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }

        Log.JsonResultExecuting(_logger, result.Value);

        var responseStream = response.Body;
        FileBufferingWriteStream? fileBufferingWriteStream = null;
        if (!_mvcOptions.SuppressOutputFormatterBuffering)
        {
            fileBufferingWriteStream = new FileBufferingWriteStream();
            responseStream = fileBufferingWriteStream;
        }

        try
        {
            var value = result.Value;
            if (value != null && _asyncEnumerableReaderFactory.TryGetReader(value.GetType(), out var reader))
            {
                Log.BufferingAsyncEnumerable(_logger, value);
                try
                {
                    value = await reader(value, context.HttpContext.RequestAborted);
                }
                catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { }
                if (context.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    return;
                }
            }

            await using (var writer = _writerFactory.CreateWriter(responseStream, resolvedContentTypeEncoding))
            {
                using var jsonWriter = new JsonTextWriter(writer);
                jsonWriter.ArrayPool = _charPool;
                jsonWriter.CloseOutput = false;
                jsonWriter.AutoCompleteOnClose = false;

                var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);

                jsonSerializer.Serialize(jsonWriter, value);
            }

            if (fileBufferingWriteStream != null)
            {
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

    private JsonSerializerSettings GetSerializerSettings(JsonResult result)
    {
        var serializerSettings = result.SerializerSettings;
        if (serializerSettings == null)
        {
            return _jsonOptions.SerializerSettings;
        }
        else
        {
            if (!(serializerSettings is JsonSerializerSettings settingsFromResult))
            {
                throw new InvalidOperationException(Resources.FormatProperty_MustBeInstanceOfType(
                    nameof(JsonResult),
                    nameof(JsonResult.SerializerSettings),
                    typeof(JsonSerializerSettings)));
            }

            return settingsFromResult;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Buffering IAsyncEnumerable instance of type '{Type}'.", EventName = "BufferingAsyncEnumerable", SkipEnabledCheck = true)]
        private static partial void BufferingAsyncEnumerable(ILogger logger, string? type);

        public static void BufferingAsyncEnumerable(ILogger logger, object asyncEnumerable)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                BufferingAsyncEnumerable(logger, asyncEnumerable.GetType().FullName);
            }
        }

        [LoggerMessage(2, LogLevel.Information, "Executing JsonResult, writing value of type '{Type}'.", EventName = "JsonResultExecuting", SkipEnabledCheck = true)]
        private static partial void JsonResultExecuting(ILogger logger, string? type);

        public static void JsonResultExecuting(ILogger logger, object? value)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var type = value == null ? "null" : value.GetType().FullName;
                JsonResultExecuting(logger, type);
            }
        }
    }
}
