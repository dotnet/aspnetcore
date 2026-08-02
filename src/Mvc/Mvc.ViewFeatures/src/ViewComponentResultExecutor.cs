// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A <see cref="IActionResultExecutor{ViewComponentResult}"/> for <see cref="ViewComponentResult"/>.
/// </summary>
public partial class ViewComponentResultExecutor : IActionResultExecutor<ViewComponentResult>
{
    private readonly HtmlEncoder _htmlEncoder;
    private readonly HtmlHelperOptions _htmlHelperOptions;
    private readonly ILogger<ViewComponentResult> _logger;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;

    /// <summary>
    /// Initialize a new instance of <see cref="ViewComponentResultExecutor"/>
    /// </summary>
    /// <param name="mvcHelperOptions">The <see cref="IOptions{MvcViewOptions}"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="tempDataDictionaryFactory">The <see cref="ITempDataDictionaryFactory"/>.</param>
    /// <param name="writerFactory">The <see cref=" IHttpResponseStreamWriterFactory"/>.</param>
    public ViewComponentResultExecutor(
        IOptions<MvcViewOptions> mvcHelperOptions,
        ILoggerFactory loggerFactory,
        HtmlEncoder htmlEncoder,
        IModelMetadataProvider modelMetadataProvider,
        ITempDataDictionaryFactory tempDataDictionaryFactory,
        IHttpResponseStreamWriterFactory writerFactory)
    {
        ArgumentNullException.ThrowIfNull(mvcHelperOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(htmlEncoder);
        ArgumentNullException.ThrowIfNull(modelMetadataProvider);
        ArgumentNullException.ThrowIfNull(tempDataDictionaryFactory);

        _htmlHelperOptions = mvcHelperOptions.Value.HtmlHelperOptions;
        _logger = loggerFactory.CreateLogger<ViewComponentResult>();
        _htmlEncoder = htmlEncoder;
        _modelMetadataProvider = modelMetadataProvider;
        _tempDataDictionaryFactory = tempDataDictionaryFactory;
        _writerFactory = writerFactory;
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAsync(ActionContext context, ViewComponentResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var response = context.HttpContext.Response;

        var viewData = result.ViewData;
        if (viewData == null)
        {
            viewData = new ViewDataDictionary(_modelMetadataProvider, context.ModelState);
        }

        var tempData = result.TempData;
        if (tempData == null)
        {
            tempData = _tempDataDictionaryFactory.GetTempData(context.HttpContext);
        }

        ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
            result.ContentType,
            response.ContentType,
            (ViewExecutor.DefaultContentType, Encoding.UTF8),
            MediaType.GetEncoding,
            out var resolvedContentType,
            out var resolvedContentTypeEncoding);

        response.ContentType = resolvedContentType;

        if (result.StatusCode != null)
        {
            response.StatusCode = result.StatusCode.Value;
        }

        await using var writer = _writerFactory.CreateWriter(response.Body, resolvedContentTypeEncoding);
        var viewContext = new ViewContext(
            context,
            NullView.Instance,
            viewData,
            tempData,
            writer,
            _htmlHelperOptions);

        OnExecuting(viewContext);

        // IViewComponentHelper is stateful, we want to make sure to retrieve it every time we need it.
        var viewComponentHelper = context.HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
        (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);
        var viewComponentResult = await GetViewComponentResult(viewComponentHelper, _logger, result);

        if (viewComponentResult is ViewBuffer viewBuffer)
        {
            // In the ordinary case, DefaultViewComponentHelper will return an instance of ViewBuffer. We can simply
            // invoke WriteToAsync on it.
            await viewBuffer.WriteToAsync(writer, _htmlEncoder);
            await writer.FlushAsync();
        }
        else
        {
            await using var bufferingStream = new FileBufferingWriteStream();
            await using (var intermediateWriter = _writerFactory.CreateWriter(bufferingStream, resolvedContentTypeEncoding))
            {
                viewComponentResult.WriteTo(intermediateWriter, _htmlEncoder);
            }

            await bufferingStream.DrainBufferAsync(response.Body);
        }
    }

    private static void OnExecuting(ViewContext viewContext)
    {
        var viewDataValuesProvider = viewContext.HttpContext.Features.Get<IViewDataValuesProviderFeature>();
        viewDataValuesProvider?.ProvideViewDataValues(viewContext.ViewData);
    }

    private static Task<IHtmlContent> GetViewComponentResult(IViewComponentHelper viewComponentHelper, ILogger logger, ViewComponentResult result)
    {
        if (result.ViewComponentType == null && result.ViewComponentName == null)
        {
            throw new InvalidOperationException(Resources.FormatViewComponentResult_NameOrTypeMustBeSet(
                nameof(ViewComponentResult.ViewComponentName),
                nameof(ViewComponentResult.ViewComponentType)));
        }
        else if (result.ViewComponentType == null)
        {
            Log.ViewComponentResultExecuting(logger, result.ViewComponentName);
            return viewComponentHelper.InvokeAsync(result.ViewComponentName!, result.Arguments);
        }
        else
        {
            Log.ViewComponentResultExecuting(logger, result.ViewComponentType);
            return viewComponentHelper.InvokeAsync(result.ViewComponentType, result.Arguments);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Executing ViewComponentResult, running {ViewComponentName}.", EventName = "ViewComponentResultExecuting")]
        public static partial void ViewComponentResultExecuting(ILogger logger, string? viewComponentName);

        public static void ViewComponentResultExecuting(ILogger logger, Type viewComponentType)
        {
            ViewComponentResultExecuting(logger, viewComponentType.Name);
        }
    }
}
