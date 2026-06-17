// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class DefaultValidationHtmlAttributeProviderTest
{
    [Fact]
    [ReplaceCulture]
    public void AddValidationAttributes_AddsAttributes()
    {
        // Arrange
        var expectedMessage = $"The field {nameof(Model.HasValidatorsProperty)} must be a number.";
        var metadataProvider = new EmptyModelMetadataProvider();
        var attributeProvider = GetAttributeProvider(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        // Act
        attributeProvider.AddValidationAttributes(
            viewContext,
            modelExplorer,
            attributes);

        // Assert
        Assert.Collection(
            attributes,
            kvp =>
            {
                Assert.Equal("data-val", kvp.Key);
                Assert.Equal("true", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-number", kvp.Key);
                Assert.Equal(expectedMessage, kvp.Value);
            });
    }

    [Fact]
    [ReplaceCulture]
    public void AddAndTrackValidationAttributes_AddsAttributes()
    {
        // Arrange
        var expectedMessage = $"The field {nameof(Model.HasValidatorsProperty)} must be a number.";
        var metadataProvider = new EmptyModelMetadataProvider();
        var attributeProvider = GetAttributeProvider(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        // Act
        attributeProvider.AddAndTrackValidationAttributes(
            viewContext,
            modelExplorer,
            nameof(Model.HasValidatorsProperty),
            attributes);

        // Assert
        Assert.Collection(
            attributes,
            kvp =>
            {
                Assert.Equal("data-val", kvp.Key);
                Assert.Equal("true", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-number", kvp.Key);
                Assert.Equal(expectedMessage, kvp.Value);
            });
    }

    [Fact]
    public void AddValidationAttributes_AddsNothing_IfClientSideValidationDisabled()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var attributeProvider = GetAttributeProvider(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.ClientValidationEnabled = false;

        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        // Act
        attributeProvider.AddValidationAttributes(
            viewContext,
            modelExplorer,
            attributes);

        // Assert
        Assert.Empty(attributes);
    }

    [Fact]
    public void AddAndTrackValidationAttributes_DoesNotCallAddMethod_IfClientSideValidationDisabled()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.ClientValidationEnabled = false;

        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        var attributeProviderMock = new Mock<ValidationHtmlAttributeProvider>() { CallBase = true };
        attributeProviderMock
            .Setup(p => p.AddValidationAttributes(
                It.IsAny<ViewContext>(),
                It.IsAny<ModelExplorer>(),
                It.IsAny<IDictionary<string, string>>()))
            .Verifiable();
        var attributeProvider = attributeProviderMock.Object;

        // Act
        attributeProvider.AddAndTrackValidationAttributes(
            viewContext,
            modelExplorer,
            nameof(Model.HasValidatorsProperty),
            attributes);

        // Assert
        Assert.Empty(attributes);
        attributeProviderMock.Verify(
            p => p.AddValidationAttributes(
                It.IsAny<ViewContext>(),
                It.IsAny<ModelExplorer>(),
                It.IsAny<IDictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public void AddValidationAttributes_AddsAttributes_EvenIfPropertyAlreadyRendered()
    {
        // Arrange
        var expectedMessage = $"The field {nameof(Model.HasValidatorsProperty)} must be a number.";
        var metadataProvider = new EmptyModelMetadataProvider();
        var attributeProvider = GetAttributeProvider(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.FormContext.RenderedField(nameof(Model.HasValidatorsProperty), value: true);

        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        // Act
        attributeProvider.AddValidationAttributes(
            viewContext,
            modelExplorer,
            attributes);

        // Assert
        Assert.Collection(
            attributes,
            kvp =>
            {
                Assert.Equal("data-val", kvp.Key);
                Assert.Equal("true", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("data-val-number", kvp.Key);
                Assert.Equal(expectedMessage, kvp.Value);
            });
    }

    [Fact]
    public void AddAndTrackValidationAttributes_DoesNotCallAddMethod_IfPropertyAlreadyRendered()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        viewContext.FormContext.RenderedField(nameof(Model.HasValidatorsProperty), value: true);

        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.HasValidatorsProperty));

        var attributeProviderMock = new Mock<ValidationHtmlAttributeProvider>() { CallBase = true };
        attributeProviderMock
            .Setup(p => p.AddValidationAttributes(
                It.IsAny<ViewContext>(),
                It.IsAny<ModelExplorer>(),
                It.IsAny<IDictionary<string, string>>()))
            .Verifiable();
        var attributeProvider = attributeProviderMock.Object;

        // Act
        attributeProvider.AddAndTrackValidationAttributes(
            viewContext,
            modelExplorer,
            nameof(Model.HasValidatorsProperty),
            attributes);

        // Assert
        Assert.Empty(attributes);
        attributeProviderMock.Verify(
            p => p.AddValidationAttributes(
                It.IsAny<ViewContext>(),
                It.IsAny<ModelExplorer>(),
                It.IsAny<IDictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public void AddValidationAttributes_AddsNothing_IfPropertyHasNoValidators()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var attributeProvider = GetAttributeProvider(metadataProvider);
        var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var modelExplorer = metadataProvider
            .GetModelExplorerForType(typeof(Model), model: null)
            .GetExplorerForProperty(nameof(Model.Property));

        // Act
        attributeProvider.AddValidationAttributes(
            viewContext,
            modelExplorer,
            attributes);

        // Assert
        Assert.Empty(attributes);
    }

    private static ViewContext GetViewContext<TModel>(TModel model, IModelMetadataProvider metadataProvider)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary<TModel>(metadataProvider, actionContext.ModelState)
        {
            Model = model,
        };

        return new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            viewData,
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }

    private static ValidationHtmlAttributeProvider GetAttributeProvider(IModelMetadataProvider metadataProvider)
    {
        // Add validation properties for float, double and decimal properties. Ignore everything else.
        var mvcViewOptions = new MvcViewOptions();
        mvcViewOptions.ClientModelValidatorProviders.Add(new NumericClientModelValidatorProvider());

        var mvcViewOptionsAccessor = new Mock<IOptions<MvcViewOptions>>();
        mvcViewOptionsAccessor.SetupGet(accessor => accessor.Value).Returns(mvcViewOptions);

        return new DefaultValidationHtmlAttributeProvider(
            mvcViewOptionsAccessor.Object,
            metadataProvider,
            new ClientValidatorCache());
    }

    private class Model
    {
        public double HasValidatorsProperty { get; set; }

        public string Property { get; set; }
    }
}
