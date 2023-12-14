// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Executes an <see cref="ObjectResult"/> to write to the response.
/// </summary>
public partial class ObjectResultExecutor : IActionResultExecutor<ObjectResult>
{
    /// <summary>
    /// Creates a new <see cref="ObjectResultExecutor"/>.
    /// </summary>
    /// <param name="formatterSelector">The <see cref="OutputFormatterSelector"/>.</param>
    /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
    public ObjectResultExecutor(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory,
        ILoggerFactory loggerFactory,
        IOptions<MvcOptions> mvcOptions)
    {
        ArgumentNullException.ThrowIfNull(formatterSelector);
        ArgumentNullException.ThrowIfNull(writerFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        FormatterSelector = formatterSelector;
        WriterFactory = writerFactory.CreateWriter;
        Logger = loggerFactory.CreateLogger<ObjectResultExecutor>();
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the <see cref="OutputFormatterSelector"/>.
    /// </summary>
    protected OutputFormatterSelector FormatterSelector { get; }

    /// <summary>
    /// Gets the writer factory delegate.
    /// </summary>
    protected Func<Stream, Encoding, TextWriter> WriterFactory { get; }

    /// <summary>
    /// Executes the <see cref="ObjectResult"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> for the current request.</param>
    /// <param name="result">The <see cref="ObjectResult"/>.</param>
    /// <returns>
    /// A <see cref="Task"/> which will complete once the <see cref="ObjectResult"/> is written to the response.
    /// </returns>
    public virtual Task ExecuteAsync(ActionContext context, ObjectResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        InferContentTypes(context, result);

        var objectType = result.DeclaredType;

        if (objectType == null || objectType == typeof(object))
        {
            objectType = result.Value?.GetType();
        }

        var value = result.Value;
        return ExecuteAsyncCore(context, result, objectType, value);
    }

    private Task ExecuteAsyncCore(ActionContext context, ObjectResult result, Type? objectType, object? value)
    {
        var formatterContext = new OutputFormatterWriteContext(
            context.HttpContext,
            WriterFactory,
            objectType,
            value);

        var selectedFormatter = FormatterSelector.SelectFormatter(
            formatterContext,
            (IList<IOutputFormatter>)result.Formatters ?? Array.Empty<IOutputFormatter>(),
            result.ContentTypes);

        if (selectedFormatter == null)
        {
            // No formatter supports this.
            Log.NoFormatter(Logger, formatterContext, result.ContentTypes);

            const int statusCode = StatusCodes.Status406NotAcceptable;
            context.HttpContext.Response.StatusCode = statusCode;

            if (context.HttpContext.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
            {
                return problemDetailsService.TryWriteAsync(new()
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = { Status = statusCode }
                }).AsTask();
            }

            return Task.CompletedTask;
        }

        Log.ObjectResultExecuting(Logger, result, value);

        result.OnFormatting(context);
        return selectedFormatter.WriteAsync(formatterContext);
    }

    private static void InferContentTypes(ActionContext context, ObjectResult result)
    {
        Debug.Assert(result.ContentTypes != null);

        // If the user sets the content type both on the ObjectResult (example: by Produces) and Response object,
        // then the one set on ObjectResult takes precedence over the Response object
        var responseContentType = context.HttpContext.Response.ContentType;
        if (result.ContentTypes.Count == 0 && !string.IsNullOrEmpty(responseContentType))
        {
            result.ContentTypes.Add(responseContentType);
        }

        if (result.Value is ProblemDetails)
        {
            result.ContentTypes.Add("application/problem+json");
            result.ContentTypes.Add("application/problem+xml");
        }
    }

    // Internal for unit testing
    internal static partial class Log
    {
        // Removed Log.
        // new EventId(1, "BufferingAsyncEnumerable")

        public static void ObjectResultExecuting(ILogger logger, ObjectResult result, object? value)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var objectResultType = result.GetType().Name;
                var valueType = value == null ? "null" : value.GetType().FullName;
                ObjectResultExecuting(logger, objectResultType, valueType);
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Executing {ObjectResultType}, writing value of type '{Type}'.", EventName = "ObjectResultExecuting", SkipEnabledCheck = true)]
        private static partial void ObjectResultExecuting(ILogger logger, string objectResultType, string? type);

        public static void NoFormatter(ILogger logger, OutputFormatterCanWriteContext context, MediaTypeCollection contentTypes)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                var considered = new List<string?>(contentTypes);

                if (context.ContentType.HasValue)
                {
                    considered.Add(Convert.ToString(context.ContentType, CultureInfo.InvariantCulture));
                }

                NoFormatter(logger, considered);
            }
        }

        [LoggerMessage(2, LogLevel.Warning, "No output formatter was found for content types '{ContentTypes}' to write the response.", EventName = "NoFormatter", SkipEnabledCheck = true)]
        private static partial void NoFormatter(ILogger logger, List<string?> contentTypes);
    }
}
