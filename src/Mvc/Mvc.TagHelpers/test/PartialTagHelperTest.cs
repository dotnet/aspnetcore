// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class PartialTagHelperTest
{
    [Fact]
    public void ResolveModel_ReturnsModelWhenProvided()
    {
        // Arrange
        var expectedModel = new object();
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            Model = expectedModel,
        };

        // Act
        var model = tagHelper.ResolveModel();

        // Assert
        Assert.Same(expectedModel, model);
    }

    [Fact]
    public void ResolveModel_ReturnsModelWhenNullValueIsProvided()
    {
        // Regression test for https://github.com/aspnet/Mvc/issues/7667.
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = new object(),
        };
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            Model = null,
            ViewData = viewData,
        };

        // Act
        var model = tagHelper.ResolveModel();

        // Assert
        Assert.Null(model);
    }

    [Fact]
    public void ResolveModel_ReturnsForModelWhenProvided()
    {
        // Arrange
        var expectedModel = new PropertyModel();
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel()
        {
            Property = expectedModel
        };
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            For = new ModelExpression("Property", propertyModelExplorer),
        };

        // Act
        var model = tagHelper.ResolveModel();

        // Assert
        Assert.Same(expectedModel, model);
    }

    [Fact]
    public void ResolveModel_ReturnsViewContextsViewDataModelWhenModelAndForAreNotSet()
    {
        // Arrange
        var expectedModel = new object();
        var viewContext = GetViewContext();
        viewContext.ViewData.Model = expectedModel;
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            ViewContext = viewContext
        };

        // Act
        var model = tagHelper.ResolveModel();

        // Assert
        Assert.Same(expectedModel, model);
    }

    [Fact]
    public void ResolveModel_ThrowsWhenModelAndForProvided()
    {
        // Arrange
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel();
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            Model = new object(),
            For = new ModelExpression("Property", propertyModelExplorer),
        };
        var expectedMessage = Resources.FormatPartialTagHelper_InvalidModelAttributes(typeof(PartialTagHelper).FullName, "for", "model");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tagHelper.ResolveModel());
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void ResolveModel_ThrowsWhenNullModelAndForProvided()
    {
        // Arrange
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel();
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));
        var tagHelper = new PartialTagHelper(Mock.Of<ICompositeViewEngine>(), Mock.Of<IViewBufferScope>())
        {
            Model = null,
            For = new ModelExpression("Property", propertyModelExplorer),
        };
        var expectedMessage = Resources.FormatPartialTagHelper_InvalidModelAttributes(typeof(PartialTagHelper).FullName, "for", "model");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => tagHelper.ResolveModel());
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_RendersPartialView_IfGetViewReturnsView()
    {
        // Arrange
        var expected = "Hello world!";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_RendersPartialView_IfFindViewReturnsView()
    {
        // Arrange
        var expected = "Hello world!";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { partialName }));

        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_UsesViewDataFromContext()
    {
        // Arrange
        var expected = "Implicit";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var viewContext = GetViewContext();
        viewContext.ViewData["key"] = expected;

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(v.ViewData["key"]);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { partialName }));

        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_UsesPassedInViewData_WhenNotNull()
    {
        // Arrange
        var expected = "Explicit";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var model = new object();
        var viewData = new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary());
        viewData["key"] = expected;
        var viewContext = GetViewContext();
        viewContext.ViewData["key"] = "ViewContext";

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(v.ViewData["key"]);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            ViewData = viewData,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_UsesModelExpression_ToDetermineModel()
    {
        // Arrange
        var expected = new PropertyModel();
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel { Property = expected };
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));

        var modelExpression = new ModelExpression("Property", propertyModelExplorer);
        var viewContext = GetViewContext();
        viewContext.ViewData.Model = new object();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                var actual = Assert.IsType<PropertyModel>(v.ViewData.Model);
                Assert.Same(expected, actual);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            For = modelExpression,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        view.Verify();
    }

    [Fact]
    public async Task ProcessAsync_DoesNotUseModelFromViewdata_IfModelExpressionEvaluatesToNull()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel { Property = null };
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));

        var modelExpression = new ModelExpression("Property", propertyModelExplorer);
        var viewContext = GetViewContext();
        viewContext.ViewData.Model = new object();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                Assert.Null(v.ViewData.Model);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            For = modelExpression,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        view.Verify();
    }

    [Fact]
    public async Task ProcessAsync_SetsHtmlFieldPrefix_UsingModelExpression()
    {
        // Arrange
        var expected = "order.items[0].Property";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var modelMetadataProvider = new TestModelMetadataProvider();
        var containerModel = new TestModel { Property = new PropertyModel() };
        var containerModelExplorer = modelMetadataProvider.GetModelExplorerForType(
            typeof(TestModel),
            containerModel);
        var propertyModelExplorer = containerModelExplorer.GetExplorerForProperty(nameof(TestModel.Property));

        var modelExpression = new ModelExpression("Property", propertyModelExplorer);
        var viewContext = GetViewContext();
        viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = "order.items[0]";

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                Assert.Equal(expected, v.ViewData.TemplateInfo.HtmlFieldPrefix);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            For = modelExpression,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        view.Verify();
        Assert.Equal("order.items[0]", viewContext.ViewData.TemplateInfo.HtmlFieldPrefix);
    }

    [Fact]
    public async Task ProcessAsync_UsesModelOnViewContextViewData_WhenModelExpressionIsNull()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var model = new object();
        var viewContext = GetViewContext();
        viewContext.ViewData.Model = model;

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                Assert.Same(model, v.ViewData.Model);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        view.Verify();
    }

    [Fact]
    public async Task ProcessAsync_DoesNotModifyHtmlFieldPrefix_WhenModelExpressionIsNull()
    {
        // Arrange
        var expected = "original";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var model = new object();
        var viewContext = GetViewContext();
        viewContext.ViewData.Model = model;
        viewContext.ViewData.TemplateInfo.HtmlFieldPrefix = expected;

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                Assert.Equal(expected, v.ViewData.TemplateInfo.HtmlFieldPrefix);
            })
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        view.Verify();
    }

    [Fact]
    public async Task ProcessAsync_DisposesViewInstance()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var viewContext = GetViewContext();

        var disposable = new Mock<IDisposable>();
        disposable.Setup(d => d.Dispose()).Verifiable();
        var view = disposable.As<IView>();

        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        disposable.Verify();
        view.Verify();
    }

    [Fact]
    public async Task ProcessAsync_Throws_If_NotOptional_And_GetViewAndFindReturnNotFoundResults()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var expected = string.Join(Environment.NewLine,
            $"The partial view '{partialName}' was not found. The following locations were searched:",
            "NotFound1",
            "NotFound2",
            "NotFound3",
            "NotFound4");
        var viewData = new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary());
        var viewContext = GetViewContext();

        var view = Mock.Of<IView>();
        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { "NotFound1", "NotFound2" }));

        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { $"NotFound3", $"NotFound4" }));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            ViewData = viewData,
            Optional = false
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => tagHelper.ProcessAsync(tagHelperContext, output));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_IfOptional_And_ViewIsNotFound_WillNotRenderAnything()
    {
        // Arrange
        var expected = string.Empty;
        var bufferScope = new TestViewBufferScope();
        var partialName = "_ThisViewDoesNotExists";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, searchedLocations: Array.Empty<string>()));
        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, searchedLocations: Array.Empty<string>()));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            Optional = true
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Empty(content);
    }

    [Fact]
    public async Task ProcessAsync_RendersMainPartial_If_FallbackIsSet_AndMainPartialIsFound()
    {
        // Arrange
        var expected = "Hello from partial!";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var fallbackName = "_Fallback";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var fallbackView = new Mock<IView>();
        fallbackView.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write("Hello from fallback partial!");
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.Found(partialName, view.Object));
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), fallbackName, false))
            .Returns(ViewEngineResult.Found(fallbackName, fallbackView.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            FallbackName = fallbackName
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_IfHasFallback_Throws_When_MainPartialAndFallback_AreNotFound()
    {
        // Arrange
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var fallbackName = "_Fallback";
        var expected = string.Join(
            Environment.NewLine,
            $"The partial view '{partialName}' was not found. The following locations were searched:",
            "PartialNotFound1",
            "PartialNotFound2",
            "PartialNotFound3",
            "PartialNotFound4",
            $"The fallback partial view '{fallbackName}' was not found. The following locations were searched:",
            "FallbackNotFound1",
            "FallbackNotFound2",
            "FallbackNotFound3",
            "FallbackNotFound4");
        var viewData = new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary());
        var viewContext = GetViewContext();

        var view = Mock.Of<IView>();
        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { "PartialNotFound1", "PartialNotFound2" }));

        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { $"PartialNotFound3", $"PartialNotFound4" }));

        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), fallbackName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { "FallbackNotFound1", "FallbackNotFound2" }));

        viewEngine.Setup(v => v.FindView(viewContext, fallbackName, false))
            .Returns(ViewEngineResult.NotFound(partialName, new[] { $"FallbackNotFound3", $"FallbackNotFound4" }));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            ViewData = viewData,
            FallbackName = fallbackName
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => tagHelper.ProcessAsync(tagHelperContext, output));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_RendersFallbackView_If_MainIsNotFound_AndGetViewReturnsView()
    {
        // Arrange
        var expected = "Hello from fallback!";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var fallbackName = "_Fallback";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, Array.Empty<string>()));
        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, Array.Empty<string>()));
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), fallbackName, false))
            .Returns(ViewEngineResult.Found(fallbackName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            FallbackName = fallbackName
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ProcessAsync_RendersFallbackView_If_MainIsNotFound_AndFindViewReturnsView()
    {
        // Arrange
        var expected = "Hello from fallback!";
        var bufferScope = new TestViewBufferScope();
        var partialName = "_Partial";
        var fallbackName = "_Fallback";
        var model = new object();
        var viewContext = GetViewContext();

        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) =>
            {
                v.Writer.Write(expected);
            })
            .Returns(Task.CompletedTask);

        var viewEngine = new Mock<ICompositeViewEngine>();
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, Array.Empty<string>()));
        viewEngine.Setup(v => v.FindView(viewContext, partialName, false))
            .Returns(ViewEngineResult.NotFound(partialName, Array.Empty<string>()));
        viewEngine.Setup(v => v.GetView(It.IsAny<string>(), fallbackName, false))
            .Returns(ViewEngineResult.NotFound(fallbackName, Array.Empty<string>()));
        viewEngine.Setup(v => v.FindView(viewContext, fallbackName, false))
            .Returns(ViewEngineResult.Found(fallbackName, view.Object));

        var tagHelper = new PartialTagHelper(viewEngine.Object, bufferScope)
        {
            Name = partialName,
            ViewContext = viewContext,
            FallbackName = fallbackName
        };
        var tagHelperContext = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content, new HtmlTestEncoder());
        Assert.Equal(expected, content);
    }

    private static ViewContext GetViewContext()
    {
        return new ViewContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            NullView.Instance,
            new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    private static TagHelperContext GetTagHelperContext()
    {
        return new TagHelperContext(
            "partial",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput GetTagHelperOutput()
    {
        return new TagHelperOutput(
            "partial",
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private class TestModel
    {
        public PropertyModel Property { get; set; }
    }

    private class PropertyModel
    {
    }
}
