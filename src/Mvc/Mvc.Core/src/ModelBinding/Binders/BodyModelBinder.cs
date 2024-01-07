// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
/// when a model has the binding source <see cref="BindingSource.Body"/>.
/// </summary>
public partial class BodyModelBinder : IModelBinder
{
    private readonly IList<IInputFormatter> _formatters;
    private readonly Func<Stream, Encoding, TextReader> _readerFactory;
    private readonly ILogger _logger;
    private readonly MvcOptions? _options;

    /// <summary>
    /// Creates a new <see cref="BodyModelBinder"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">
    /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
    /// instances for reading the request body.
    /// </param>
    public BodyModelBinder(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
        : this(formatters, readerFactory, loggerFactory: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BodyModelBinder"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">
    /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
    /// instances for reading the request body.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public BodyModelBinder(
        IList<IInputFormatter> formatters,
        IHttpRequestStreamReaderFactory readerFactory,
        ILoggerFactory? loggerFactory)
        : this(formatters, readerFactory, loggerFactory, options: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BodyModelBinder"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">
    /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
    /// instances for reading the request body.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    public BodyModelBinder(
        IList<IInputFormatter> formatters,
        IHttpRequestStreamReaderFactory readerFactory,
        ILoggerFactory? loggerFactory,
        MvcOptions? options)
    {
        ArgumentNullException.ThrowIfNull(formatters);
        ArgumentNullException.ThrowIfNull(readerFactory);

        _formatters = formatters;
        _readerFactory = readerFactory.CreateReader;

        _logger = loggerFactory?.CreateLogger(typeof(BodyModelBinder)) ?? NullLogger<BodyModelBinder>.Instance;

        _options = options;
    }

    internal bool AllowEmptyBody { get; set; }

    /// <inheritdoc />
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        _logger.AttemptingToBindModel(bindingContext);

        // Special logic for body, treat the model name as string.Empty for the top level
        // object, but allow an override via BinderModelName. The purpose of this is to try
        // and be similar to the behavior for POCOs bound via traditional model binding.
        string modelBindingKey;
        if (bindingContext.IsTopLevelObject)
        {
            modelBindingKey = bindingContext.BinderModelName ?? string.Empty;
        }
        else
        {
            modelBindingKey = bindingContext.ModelName;
        }

        var httpContext = bindingContext.HttpContext;

        var formatterContext = new InputFormatterContext(
            httpContext,
            modelBindingKey,
            bindingContext.ModelState,
            bindingContext.ModelMetadata,
            _readerFactory,
            AllowEmptyBody);

        var formatter = (IInputFormatter?)null;
        for (var i = 0; i < _formatters.Count; i++)
        {
            if (_formatters[i].CanRead(formatterContext))
            {
                formatter = _formatters[i];
                Log.InputFormatterSelected(_logger, formatter, formatterContext);
                break;
            }
            else
            {
                Log.InputFormatterRejected(_logger, _formatters[i], formatterContext);
            }
        }

        if (formatter == null)
        {
            if (AllowEmptyBody)
            {
                var hasBody = httpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
                hasBody ??= httpContext.Request.ContentLength is not null && httpContext.Request.ContentLength == 0;
                if (hasBody == false)
                {
                    bindingContext.Result = ModelBindingResult.Success(model: null);
                    return;
                }
            }

            Log.NoInputFormatterSelected(_logger, formatterContext);

            var message = Resources.FormatUnsupportedContentType(httpContext.Request.ContentType);
            var exception = new UnsupportedContentTypeException(message);
            bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
            _logger.DoneAttemptingToBindModel(bindingContext);
            return;
        }

        try
        {
            var result = await formatter.ReadAsync(formatterContext);

            if (result.HasError)
            {
                // Formatter encountered an error. Do not use the model it returned.
                _logger.DoneAttemptingToBindModel(bindingContext);
                return;
            }

            if (result.IsModelSet)
            {
                var model = result.Model;
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            else
            {
                // If the input formatter gives a "no value" result, that's always a model state error,
                // because BodyModelBinder implicitly regards input as being required for model binding.
                // If instead the input formatter wants to treat the input as optional, it must do so by
                // returning InputFormatterResult.Success(defaultForModelType), because input formatters
                // are responsible for choosing a default value for the model type.
                var message = bindingContext
                    .ModelMetadata
                    .ModelBindingMessageProvider
                    .MissingRequestBodyRequiredValueAccessor();
                bindingContext.ModelState.AddModelError(modelBindingKey, message);
            }
        }
        catch (Exception exception) when (exception is InputFormatterException || ShouldHandleException(formatter))
        {
            bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
    }

    private static bool ShouldHandleException(IInputFormatter formatter)
    {
        // Any explicit policy on the formatters overrides the default.
        var policy = (formatter as IInputFormatterExceptionPolicy)?.ExceptionPolicy ??
            InputFormatterExceptionPolicy.MalformedInputExceptions;

        return policy == InputFormatterExceptionPolicy.AllExceptions;
    }

    private sealed partial class Log
    {
        public static void InputFormatterSelected(ILogger logger, IInputFormatter inputFormatter, InputFormatterContext formatterContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var contentType = formatterContext.HttpContext.Request.ContentType;
                InputFormatterSelected(logger, inputFormatter, contentType);
            }
        }

        [LoggerMessage(1, LogLevel.Debug, "Selected input formatter '{InputFormatter}' for content type '{ContentType}'.", EventName = "InputFormatterSelected", SkipEnabledCheck = true)]
        private static partial void InputFormatterSelected(ILogger logger, IInputFormatter inputFormatter, string? contentType);

        public static void InputFormatterRejected(ILogger logger, IInputFormatter inputFormatter, InputFormatterContext formatterContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var contentType = formatterContext.HttpContext.Request.ContentType;
                InputFormatterRejected(logger, inputFormatter, contentType);
            }
        }

        [LoggerMessage(2, LogLevel.Debug, "Rejected input formatter '{InputFormatter}' for content type '{ContentType}'.", EventName = "InputFormatterRejected", SkipEnabledCheck = true)]
        private static partial void InputFormatterRejected(ILogger logger, IInputFormatter inputFormatter, string? contentType);

        public static void NoInputFormatterSelected(ILogger logger, InputFormatterContext formatterContext)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var contentType = formatterContext.HttpContext.Request.ContentType;
                NoInputFormatterSelected(logger, contentType);
                if (formatterContext.HttpContext.Request.HasFormContentType)
                {
                    var modelType = formatterContext.ModelType.FullName;
                    var modelName = formatterContext.ModelName;
                    RemoveFromBodyAttribute(logger, modelName, modelType);
                }
            }
        }

        [LoggerMessage(3, LogLevel.Debug, "No input formatter was found to support the content type '{ContentType}' for use with the [FromBody] attribute.", EventName = "NoInputFormatterSelected", SkipEnabledCheck = true)]
        private static partial void NoInputFormatterSelected(ILogger logger, string? contentType);

        [LoggerMessage(4, LogLevel.Debug, "To use model binding, remove the [FromBody] attribute from the property or parameter named '{ModelName}' with model type '{ModelType}'.", EventName = "RemoveFromBodyAttribute", SkipEnabledCheck = true)]
        private static partial void RemoveFromBodyAttribute(ILogger logger, string modelName, string? modelType);
    }
}
