// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ViewComponentResultExecutor
    {
        private readonly HtmlEncoder _htmlEncoder;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly ILogger<ViewComponentResult> _logger;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;

        public ViewComponentResultExecutor(
            IOptions<MvcViewOptions> mvcHelperOptions,
            ILoggerFactory loggerFactory,
            HtmlEncoder htmlEncoder,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataDictionaryFactory)
        {
            if (mvcHelperOptions == null)
            {
                throw new ArgumentNullException(nameof(mvcHelperOptions));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (tempDataDictionaryFactory == null)
            {
                throw new ArgumentNullException(nameof(tempDataDictionaryFactory));
            }

            _htmlHelperOptions = mvcHelperOptions.Value.HtmlHelperOptions;
            _logger = loggerFactory.CreateLogger<ViewComponentResult>();
            _htmlEncoder = htmlEncoder;
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
        }

        public virtual async Task ExecuteAsync(ActionContext context, ViewComponentResult viewComponentResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (viewComponentResult == null)
            {
                throw new ArgumentNullException(nameof(viewComponentResult));
            }

            var response = context.HttpContext.Response;

            var viewData = viewComponentResult.ViewData;
            if (viewData == null)
            {
                viewData = new ViewDataDictionary(_modelMetadataProvider, context.ModelState);
            }

            var tempData = viewComponentResult.TempData;
            if (tempData == null)
            {
                tempData = _tempDataDictionaryFactory.GetTempData(context.HttpContext);
            }

            string resolvedContentType;
            Encoding resolvedContentTypeEncoding;
            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                viewComponentResult.ContentType,
                response.ContentType,
                ViewExecutor.DefaultContentType,
                out resolvedContentType,
                out resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (viewComponentResult.StatusCode != null)
            {
                response.StatusCode = viewComponentResult.StatusCode.Value;
            }

            using (var writer = new HttpResponseStreamWriter(response.Body, resolvedContentTypeEncoding))
            {
                var viewContext = new ViewContext(
                    context,
                    NullView.Instance,
                    viewData,
                    tempData,
                    writer,
                    _htmlHelperOptions);

                // IViewComponentHelper is stateful, we want to make sure to retrieve it every time we need it.
                var viewComponentHelper = context.HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
                (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);

                var result = await GetViewComponentResult(viewComponentHelper, _logger, viewComponentResult);

                result.WriteTo(writer, _htmlEncoder);
            }
        }

        private Task<IHtmlContent> GetViewComponentResult(IViewComponentHelper viewComponentHelper, ILogger logger, ViewComponentResult result)
        {
            if (result.ViewComponentType == null && result.ViewComponentName == null)
            {
                throw new InvalidOperationException(Resources.FormatViewComponentResult_NameOrTypeMustBeSet(
                    nameof(ViewComponentResult.ViewComponentName),
                    nameof(ViewComponentResult.ViewComponentType)));
            }
            else if (result.ViewComponentType == null)
            {
                logger.ViewComponentResultExecuting(result.ViewComponentName);
                return viewComponentHelper.InvokeAsync(result.ViewComponentName, result.Arguments);
            }
            else
            {
                logger.ViewComponentResultExecuting(result.ViewComponentType);
                return viewComponentHelper.InvokeAsync(result.ViewComponentType, result.Arguments);
            }
        }
    }
}
