// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class RazorPageCreateModelExpressionTest
{
    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IdentityExpressions_ForModelGivesM()
    {
        // m => m
        // Arrange
        var viewContext = CreateViewContext();
        var modelExplorer = viewContext.ViewData.ModelExplorer.GetExplorerForProperty(
            nameof(RazorPageCreateModelExpressionModel.Name));
        var viewData = new ViewDataDictionary<string>(viewContext.ViewData)
        {
            ModelExplorer = modelExplorer,
        };
        viewContext.ViewData = viewData;

        var page = CreateIdentityPage(viewContext);

        // Act
        var modelExpression = page.CreateModelExpression1();

        // Assert
        Assert.NotNull(modelExpression);
        Assert.Empty(modelExpression.Name);
        Assert.Same(modelExplorer, modelExpression.ModelExplorer);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IdentityExpressions_ForModelGivesModel()
    {
        // m => m.Model
        // Arrange
        var viewContext = CreateViewContext();
        var modelExplorer = viewContext.ViewData.ModelExplorer.GetExplorerForProperty(
            nameof(RazorPageCreateModelExpressionModel.Name));
        var viewData = new ViewDataDictionary<string>(viewContext.ViewData)
        {
            ModelExplorer = modelExplorer,
        };
        viewContext.ViewData = viewData;

        var page = CreateIdentityPage(viewContext);

        // Act
        var modelExpression = page.CreateModelExpression2();

        // Assert
        Assert.NotNull(modelExpression);
        Assert.Empty(modelExpression.Name);
        Assert.Same(modelExplorer, modelExpression.ModelExplorer);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_NotQuiteIdentityExpressions_ForModelGivesMDotModel()
    {
        // m => m.Model
        // Arrange
        var expectedName = "Model";
        var expectedType = typeof(RecursiveModel);

        CreateModelExpression_NotQuiteIdentityExpressions(page => page.CreateModelExpression1(), expectedName, expectedType);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_NotQuiteIdentityExpressions_ForModelGivesViewDataDotModel()
    {
        // m => ViewData.Model
        // Arrange
        var expectedName = "ViewData.Model";
        var expectedType = typeof(RecursiveModel);

        CreateModelExpression_NotQuiteIdentityExpressions(page => page.CreateModelExpression2(), expectedName, expectedType);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_NotQuiteIdentityExpressions_ForModelGivesViewContextDotViewDataDotModel()
    {
        // m => ViewContext.ViewData.Model
        // Arrange
        var expectedName = "ViewContext.ViewData.Model";
        // This property has type object because ViewData is not exposed as ViewDataDictionary<TModel>.
        var expectedType = typeof(object);

        CreateModelExpression_NotQuiteIdentityExpressions(page => page.CreateModelExpression3(), expectedName, expectedType);
    }

    private static void CreateModelExpression_NotQuiteIdentityExpressions(
        Func<NotQuiteIdentityRazorPage, ModelExpression> createModelExpression,
        string expectedName,
        Type expectedType)
    {
        var viewContext = CreateViewContext();
        var viewData = new ViewDataDictionary<RecursiveModel>(viewContext.ViewData);
        viewContext.ViewData = viewData;
        var modelExplorer = viewData.ModelExplorer;

        var page = CreateNotQuiteIdentityPage(viewContext);

        // Act
        var modelExpression = createModelExpression(page);

        // Assert
        Assert.NotNull(modelExpression);
        Assert.Equal(expectedName, modelExpression.Name);
        Assert.NotNull(modelExpression.ModelExplorer);
        Assert.NotSame(modelExplorer, modelExpression.ModelExplorer);
        Assert.NotNull(modelExpression.Metadata);
        Assert.Equal(ModelMetadataKind.Property, modelExpression.Metadata.MetadataKind);
        Assert.Equal(expectedType, modelExpression.Metadata.ModelType);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IntExpressions_ForModelGivesSomethingElse()
    {
        // Arrange
        var expected = "somethingElse";
        var somethingElse = 23;
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => somethingElse);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(int), result.Metadata.ModelType);
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IntExpressions_ForModelGivesId()
    {
        // Arrange
        var expected = "Id";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(int), result.Metadata.ModelType);
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IntExpressions_ForModelGivesSubModelId()
    {
        // Arrange
        var expected = "SubModel.Id";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.SubModel.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(int), result.Metadata.ModelType);
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_IntExpressions_ForModelGivesSubSubModelId()
    {
        // Arrange
        var expected = "SubModel.SubSubModel.Id";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.SubModel.SubSubModel.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(int), result.Metadata.ModelType);
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_StringExpressions_ForModelGivesSomethingElse()
    {
        // Arrange
        var somethingElse = "This is something else";
        var expectedName = "somethingElse";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => somethingElse);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(string), result.Metadata.ModelType);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_StringExpressions_ForModelGivesName()
    {
        // Arrange
        var expectedName = "Name";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.Name);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(string), result.Metadata.ModelType);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_StringExpressions_ForModelGivesSubmodelName()
    {
        // Arrange
        var expectedName = "SubModel.SubSubModel.Name";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.SubModel.SubSubModel.Name);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(string), result.Metadata.ModelType);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void CreateModelExpression_ReturnsExpectedMetadata_StringExpressions_ForModelGivesSubSubmodelName()
    {
        // Arrange
        var expectedName = "SubModel.Name";
        var viewContext = CreateViewContext();
        var page = CreatePage(viewContext);

        // Act
        var result = page.ModelExpressionProvider.CreateModelExpression(page.ViewData, model => model.SubModel.Name);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(typeof(string), result.Metadata.ModelType);
        Assert.Equal(expectedName, result.Name);
    }

    private static IdentityRazorPage CreateIdentityPage(ViewContext viewContext)
    {
        return new IdentityRazorPage
        {
            ViewContext = viewContext,
            ViewData = (ViewDataDictionary<string>)viewContext.ViewData,
            ModelExpressionProvider = CreateModelExpressionProvider(),
        };
    }

    public static NotQuiteIdentityRazorPage CreateNotQuiteIdentityPage(ViewContext viewContext)
    {
        return new NotQuiteIdentityRazorPage
        {
            ViewContext = viewContext,
            ViewData = (ViewDataDictionary<RecursiveModel>)viewContext.ViewData,
            ModelExpressionProvider = CreateModelExpressionProvider(),
        };
    }

    private static TestRazorPage CreatePage(ViewContext viewContext)
    {
        return new TestRazorPage
        {
            ViewContext = viewContext,
            ViewData = (ViewDataDictionary<RazorPageCreateModelExpressionModel>)viewContext.ViewData,
            ModelExpressionProvider = CreateModelExpressionProvider(),
        };
    }

    private static IModelExpressionProvider CreateModelExpressionProvider()
    {
        var provider = new EmptyModelMetadataProvider();
        var modelExpressionProvider = new ModelExpressionProvider(provider);

        return modelExpressionProvider;
    }

    private static ViewContext CreateViewContext()
    {
        var provider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary<RazorPageCreateModelExpressionModel>(provider, new ModelStateDictionary());
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IModelMetadataProvider>(provider);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceCollection.BuildServiceProvider(),
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return new ViewContext(
            actionContext,
            NullView.Instance,
            viewData,
            Mock.Of<ITempDataDictionary>(),
            new StringWriter(),
            new HtmlHelperOptions());
    }

    public class IdentityRazorPage : TestRazorPage<string>
    {
        public ModelExpression CreateModelExpression1()
        {
            return ModelExpressionProvider.CreateModelExpression(ViewData, m => m);
        }

        public ModelExpression CreateModelExpression2()
        {
            return ModelExpressionProvider.CreateModelExpression(ViewData, m => Model);
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class NotQuiteIdentityRazorPage : TestRazorPage<RecursiveModel>
    {
        public ModelExpression CreateModelExpression1()
        {
            return ModelExpressionProvider.CreateModelExpression(ViewData, m => m.Model);
        }

        public ModelExpression CreateModelExpression2()
        {
            return ModelExpressionProvider.CreateModelExpression(ViewData, m => ViewData.Model);
        }

        public ModelExpression CreateModelExpression3()
        {
            return ModelExpressionProvider.CreateModelExpression(ViewData, m => ViewContext.ViewData.Model);
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class TestRazorPage : TestRazorPage<RazorPageCreateModelExpressionModel>
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class TestRazorPage<TModel> : RazorPage<TModel>
    {
        public IModelExpressionProvider ModelExpressionProvider { get; set; }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class RecursiveModel
    {
        public RecursiveModel Model { get; set; }
    }

    public class RazorPageCreateModelExpressionModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public RazorPageCreateModelExpressionSubModel SubModel { get; set; }
    }

    public class RazorPageCreateModelExpressionSubModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public RazorPageCreateModelExpressionSubSubModel SubSubModel { get; set; }
    }

    public class RazorPageCreateModelExpressionSubSubModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
