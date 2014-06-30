// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.OptionsModel;
using Moq;

namespace Microsoft.AspNet.Mvc.Core
{
    public class DefaultTemplatesUtilities
    {
        public class ObjectTemplateModel
        {
            public ObjectTemplateModel()
            {
                ComplexInnerModel = new object();
            }

            public string Property1 { get; set; }
            public string Property2 { get; set; }
            public object ComplexInnerModel { get; set; }
        }

        public class ObjectWithScaffoldColumn
        {
            public string Property1 { get; set; }

            [ScaffoldColumn(false)]
            public string Property2 { get; set; }

            [ScaffoldColumn(true)]
            public string Property3 { get; set; }
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper()
        {
            return GetHtmlHelper<ObjectTemplateModel>(null);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model)
        {
            return GetHtmlHelper(model, CreateViewEngine());
        }

        public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IModelMetadataProvider provider)
        {
            return GetHtmlHelper<ObjectTemplateModel>(null, provider);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, IModelMetadataProvider provider)
        {
            return GetHtmlHelper(model, CreateViewEngine(), provider);
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, ICompositeViewEngine viewEngine)
        {
            return GetHtmlHelper(model, viewEngine, new DataAnnotationsModelMetadataProvider());
        }

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
            TModel model,
            ICompositeViewEngine viewEngine,
            IModelMetadataProvider provider)
        {
            var viewData = new ViewDataDictionary<TModel>(provider);
            viewData.Model = model;

            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(o => o.Response)
                .Returns(Mock.Of<HttpResponse>());
            httpContext
                .Setup(o => o.Items)
                .Returns(new Dictionary<object, object>());

            var actionContext = new ActionContext(httpContext.Object,
                                      new RouteData(),
                                      new ActionDescriptor());

            var actionBindingContext = new ActionBindingContext(actionContext,
                                                                provider,
                                                                Mock.Of<IModelBinder>(),
                                                                Mock.Of<IValueProvider>(),
                                                                Mock.Of<IInputFormatterProvider>(),
                                                                Enumerable.Empty<IModelValidatorProvider>());
            var urlHelper = Mock.Of<IUrlHelper>();
            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>();
            actionBindingContextProvider
               .Setup(c => c.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
               .Returns(Task.FromResult(actionBindingContext));

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(typeof(ICompositeViewEngine)))
                .Returns(viewEngine);
            serviceProvider
                .Setup(s => s.GetService(typeof(IUrlHelper)))
                .Returns(urlHelper);
            serviceProvider
                .Setup(s => s.GetService(typeof(IViewComponentHelper)))
                .Returns(new Mock<IViewComponentHelper>().Object);

            httpContext
                .Setup(o => o.RequestServices)
                .Returns(serviceProvider.Object);

            var viewContext = new ViewContext(actionContext, Mock.Of<IView>(), viewData, new StringWriter());

            // TemplateRenderer will Contextualize this transient service.
            serviceProvider
                .Setup(s => s.GetService(typeof(IHtmlHelper)))
                .Returns(() => new HtmlHelper(
                    viewEngine,
                    provider,
                    urlHelper,
                    GetAntiForgeryInstance(),
                    actionBindingContextProvider.Object));

            var htmlHelper = new HtmlHelper<TModel>(
                viewEngine,
                provider,
                urlHelper,
                GetAntiForgeryInstance(),
                actionBindingContextProvider.Object);
            htmlHelper.Contextualize(viewContext);

            return htmlHelper;
        }

        private static ICompositeViewEngine CreateViewEngine()
        {
            var view = new Mock<IView>();
            view
                .Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
                .Callback(async (ViewContext v) =>
                {
                    await v.Writer.WriteAsync(FormatOutput(v.ViewData.ModelMetadata));
                })
                .Returns(Task.FromResult(0));

            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.Found("MyView", view.Object));

            return viewEngine.Object;
        }

        private static AntiForgery GetAntiForgeryInstance()
        {
            var claimExtractor = new Mock<IClaimUidExtractor>();
            var dataProtectionProvider = new Mock<IDataProtectionProvider>();
            var additionalDataProvider = new Mock<IAntiForgeryAdditionalDataProvider>();
            var optionsAccessor = new Mock<IOptionsAccessor<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options).Returns(new MvcOptions());
            return new AntiForgery(claimExtractor.Object,
                                   dataProtectionProvider.Object,
                                   additionalDataProvider.Object,
                                   optionsAccessor.Object);
        }

        private static string FormatOutput(ModelMetadata metadata)
        {
            return string.Format(CultureInfo.InvariantCulture,
                                "Model = {0}, ModelType = {1}, PropertyName = {2}, SimpleDisplayText = {3}",
                                metadata.Model ?? "(null)",
                                metadata.ModelType == null ? "(null)" : metadata.ModelType.FullName,
                                metadata.PropertyName ?? "(null)",
                                metadata.SimpleDisplayText ?? "(null)");
        }
    }
}