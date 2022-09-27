// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class DefaultTemplatesUtilities
{
    public class ObjectTemplateModel
    {
        public ObjectTemplateModel()
        {
            ComplexInnerModel = new object();
        }

        public string Property1 { get; set; }
        [Display(Name = "Prop2")]
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
        return GetHtmlHelper<ObjectTemplateModel>(model: null);
    }

    public static HtmlHelper<IEnumerable<ObjectTemplateModel>> GetHtmlHelperForEnumerable()
    {
        return GetHtmlHelper<IEnumerable<ObjectTemplateModel>>(model: null);
    }

    public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IUrlHelper urlHelper)
    {
        return GetHtmlHelper<ObjectTemplateModel>(
            model: null,
            urlHelper: urlHelper,
            viewEngine: CreateViewEngine(),
            provider: TestModelMetadataProvider.CreateDefaultProvider());
    }

    public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IHtmlGenerator htmlGenerator)
    {
        var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        return GetHtmlHelper<ObjectTemplateModel>(
            new ViewDataDictionary<ObjectTemplateModel>(metadataProvider),
            CreateUrlHelper(),
            CreateViewEngine(),
            metadataProvider,
            localizerFactory: null,
            innerHelperWrapper: null,
            htmlGenerator: htmlGenerator,
            idAttributeDotReplacement: null);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(ViewDataDictionary<TModel> viewData)
    {
        return GetHtmlHelper(
            viewData,
            CreateUrlHelper(),
            CreateViewEngine(),
            TestModelMetadataProvider.CreateDefaultProvider(),
            localizerFactory: null,
            innerHelperWrapper: null,
            htmlGenerator: null,
            idAttributeDotReplacement: null);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        ViewDataDictionary<TModel> viewData,
        string idAttributeDotReplacement)
    {
        return GetHtmlHelper(
            viewData,
            CreateUrlHelper(),
            CreateViewEngine(),
            TestModelMetadataProvider.CreateDefaultProvider(),
            localizerFactory: null,
            innerHelperWrapper: null,
            htmlGenerator: null,
            idAttributeDotReplacement: idAttributeDotReplacement);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model)
    {
        return GetHtmlHelper(model, CreateViewEngine());
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, string idAttributeDotReplacement)
    {
        var provider = TestModelMetadataProvider.CreateDefaultProvider();
        var viewData = new ViewDataDictionary<TModel>(provider);
        viewData.Model = model;

        return GetHtmlHelper(
            viewData,
            CreateUrlHelper(),
            CreateViewEngine(),
            provider,
            localizerFactory: null,
            innerHelperWrapper: null,
            htmlGenerator: null,
            idAttributeDotReplacement: idAttributeDotReplacement);
    }

    public static HtmlHelper<IEnumerable<TModel>> GetHtmlHelperForEnumerable<TModel>(TModel model)
    {
        return GetHtmlHelper<IEnumerable<TModel>>(new TModel[] { model });
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(IModelMetadataProvider provider)
    {
        return GetHtmlHelper<TModel>(model: default(TModel), provider: provider);
    }

    public static HtmlHelper<ObjectTemplateModel> GetHtmlHelper(IModelMetadataProvider provider)
    {
        return GetHtmlHelper<ObjectTemplateModel>(model: null, provider: provider);
    }

    public static HtmlHelper<IEnumerable<ObjectTemplateModel>> GetHtmlHelperForEnumerable(
        IModelMetadataProvider provider)
    {
        return GetHtmlHelper<IEnumerable<ObjectTemplateModel>>(model: null, provider: provider);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(TModel model, IModelMetadataProvider provider)
    {
        return GetHtmlHelper(model, CreateUrlHelper(), CreateViewEngine(), provider);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        TModel model,
        ICompositeViewEngine viewEngine,
        IStringLocalizerFactory stringLocalizerFactory = null)
    {
        return GetHtmlHelper(
            model,
            CreateUrlHelper(),
            viewEngine,
            TestModelMetadataProvider.CreateDefaultProvider(stringLocalizerFactory),
            stringLocalizerFactory);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        TModel model,
        ICompositeViewEngine viewEngine,
        Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper)
    {
        return GetHtmlHelper(
            model,
            CreateUrlHelper(),
            viewEngine,
            TestModelMetadataProvider.CreateDefaultProvider(),
            localizerFactory: null,
            innerHelperWrapper);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        TModel model,
        IUrlHelper urlHelper,
        ICompositeViewEngine viewEngine,
        IModelMetadataProvider provider,
        IStringLocalizerFactory localizerFactory = null)
    {
        return GetHtmlHelper(model, urlHelper, viewEngine, provider, localizerFactory, innerHelperWrapper: null);
    }

    public static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        TModel model,
        IUrlHelper urlHelper,
        ICompositeViewEngine viewEngine,
        IModelMetadataProvider provider,
        IStringLocalizerFactory localizerFactory,
        Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper)
    {
        var viewData = new ViewDataDictionary<TModel>(provider);
        viewData.Model = model;

        return GetHtmlHelper(
            viewData,
            urlHelper,
            viewEngine,
            provider,
            localizerFactory,
            innerHelperWrapper,
            htmlGenerator: null,
            idAttributeDotReplacement: null);
    }

    private static HtmlHelper<TModel> GetHtmlHelper<TModel>(
        ViewDataDictionary<TModel> viewData,
        IUrlHelper urlHelper,
        ICompositeViewEngine viewEngine,
        IModelMetadataProvider provider,
        IStringLocalizerFactory localizerFactory,
        Func<IHtmlHelper, IHtmlHelper> innerHelperWrapper,
        IHtmlGenerator htmlGenerator,
        string idAttributeDotReplacement)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var options = new MvcViewOptions();
        if (!string.IsNullOrEmpty(idAttributeDotReplacement))
        {
            options.HtmlHelperOptions.IdAttributeDotReplacement = idAttributeDotReplacement;
        }

        var localizationOptions = new MvcDataAnnotationsLocalizationOptions();
        var localizationOptionsAccesor = Options.Create(localizationOptions);

        options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider(
            new ValidationAttributeAdapterProvider(),
            localizationOptionsAccesor,
            localizerFactory));

        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper);

        if (htmlGenerator == null)
        {
            htmlGenerator = HtmlGeneratorUtilities.GetHtmlGenerator(provider, urlHelperFactory.Object, options);
        }

        // TemplateRenderer will Contextualize this transient service.
        var innerHelper = (IHtmlHelper)new HtmlHelper(
            htmlGenerator,
            viewEngine,
            provider,
            new TestViewBufferScope(),
            new HtmlTestEncoder(),
            UrlEncoder.Default);

        if (innerHelperWrapper != null)
        {
            innerHelper = innerHelperWrapper(innerHelper);
        }

        var serviceProvider = new ServiceCollection()
           .AddSingleton(viewEngine)
           .AddSingleton(urlHelperFactory.Object)
           .AddSingleton(Mock.Of<IViewComponentHelper>())
           .AddSingleton(innerHelper)
           .AddSingleton<IViewBufferScope, TestViewBufferScope>()
           .BuildServiceProvider();

        httpContext.RequestServices = serviceProvider;

        var htmlHelper = new HtmlHelper<TModel>(
            htmlGenerator,
            viewEngine,
            provider,
            new TestViewBufferScope(),
            new HtmlTestEncoder(),
            UrlEncoder.Default,
            new ModelExpressionProvider(provider));

        var viewContext = new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            viewData,
            new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>()),
            new StringWriter(),
            options.HtmlHelperOptions);

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
                view.ToString();
                await v.Writer.WriteAsync(FormatOutput(v.ViewData.ModelExplorer));
            })
            .Returns(Task.FromResult(0));

        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("MyView", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("MyView", view.Object))
            .Verifiable();

        return viewEngine.Object;
    }

    public static string FormatOutput(IHtmlHelper helper, object model)
    {
        var modelExplorer = helper.MetadataProvider.GetModelExplorerForType(model.GetType(), model);
        return FormatOutput(modelExplorer);
    }

    private static string FormatOutput(ModelExplorer modelExplorer)
    {
        var metadata = modelExplorer.Metadata;
        return string.Format(
            CultureInfo.InvariantCulture,
            "Model = {0}, ModelType = {1}, PropertyName = {2}, SimpleDisplayText = {3}",
            modelExplorer.Model ?? "(null)",
            metadata.ModelType == null ? "(null)" : metadata.ModelType.FullName,
            metadata.PropertyName ?? "(null)",
            modelExplorer.GetSimpleDisplayText() ?? "(null)");
    }

    private static IUrlHelper CreateUrlHelper()
    {
        return Mock.Of<IUrlHelper>();
    }
}
