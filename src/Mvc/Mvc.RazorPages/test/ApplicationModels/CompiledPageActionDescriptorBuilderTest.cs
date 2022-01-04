// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class CompiledPageActionDescriptorBuilderTest
{
    [Fact]
    public void CreateDescriptor_CopiesPropertiesFromPageActionDescriptor()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor
        {
            ActionConstraints = new List<IActionConstraintMetadata>(),
            AttributeRouteInfo = new AttributeRouteInfo(),
            EndpointMetadata = new List<object>(),
            FilterDescriptors = new List<FilterDescriptor>(),
            RelativePath = "/Foo",
            RouteValues = new Dictionary<string, string>(),
            ViewEnginePath = "/Pages/Foo",
        };
        var handlerTypeInfo = typeof(object).GetTypeInfo();
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0]);
        var globalFilters = new FilterCollection();

        // Act
        var actual = CompiledPageActionDescriptorBuilder.Build(pageApplicationModel, globalFilters);

        // Assert
        Assert.Same(actionDescriptor.ActionConstraints, actual.ActionConstraints);
        Assert.Same(actionDescriptor.AttributeRouteInfo, actual.AttributeRouteInfo);
        Assert.Same(actionDescriptor.RelativePath, actual.RelativePath);
        Assert.Same(actionDescriptor.RouteValues, actual.RouteValues);
        Assert.Same(actionDescriptor.ViewEnginePath, actual.ViewEnginePath);
    }

    [Fact]
    public void CreateDescriptor_CopiesPropertiesFromPageApplicationModel()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor
        {
            ActionConstraints = new List<IActionConstraintMetadata>(),
            AttributeRouteInfo = new AttributeRouteInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            RelativePath = "/Foo",
            RouteValues = new Dictionary<string, string>(),
            ViewEnginePath = "/Pages/Foo",
        };
        var handlerTypeInfo = typeof(TestModel).GetTypeInfo();
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, typeof(TestModel).GetTypeInfo(), handlerTypeInfo, new object[0])
        {
            PageType = typeof(TestPage).GetTypeInfo(),
            ModelType = typeof(TestModel).GetTypeInfo(),
            Filters =
                {
                    Mock.Of<IFilterMetadata>(),
                    Mock.Of<IFilterMetadata>(),
                },
            HandlerMethods =
                {
                    new PageHandlerModel(handlerTypeInfo.GetMethod(nameof(TestModel.OnGet)), new object[0]),
                },
            HandlerProperties =
                {
                    new PagePropertyModel(handlerTypeInfo.GetProperty(nameof(TestModel.Property)), new object[0])
                    {
                        BindingInfo = new BindingInfo(),
                    },
                }
        };
        var globalFilters = new FilterCollection();

        // Act
        var actual = CompiledPageActionDescriptorBuilder.Build(pageApplicationModel, globalFilters);

        // Assert
        Assert.Same(pageApplicationModel.PageType, actual.PageTypeInfo);
        Assert.Same(pageApplicationModel.DeclaredModelType, actual.DeclaredModelTypeInfo);
        Assert.Same(pageApplicationModel.ModelType, actual.ModelTypeInfo);
        Assert.Same(pageApplicationModel.HandlerType, actual.HandlerTypeInfo);
        Assert.Same(pageApplicationModel.Properties, actual.Properties);
        Assert.Equal(pageApplicationModel.Filters, actual.FilterDescriptors.Select(f => f.Filter));
        Assert.Equal(pageApplicationModel.HandlerMethods.Select(p => p.MethodInfo), actual.HandlerMethods.Select(p => p.MethodInfo));
        Assert.Equal(pageApplicationModel.HandlerProperties.Select(p => p.PropertyName), actual.BoundProperties.Select(p => p.Name));
    }

    [Fact]
    public void CreateDescriptor_ThrowsIfModelIsNotCompatibleWithDeclaredModel()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor
        {
            ActionConstraints = new List<IActionConstraintMetadata>(),
            AttributeRouteInfo = new AttributeRouteInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            RelativePath = "/Foo",
            RouteValues = new Dictionary<string, string>(),
            ViewEnginePath = "/Pages/Foo",
        };
        var handlerTypeInfo = typeof(TestModel).GetTypeInfo();
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, typeof(TestModel).GetTypeInfo(), handlerTypeInfo, new object[0])
        {
            PageType = typeof(TestPage).GetTypeInfo(),
            ModelType = typeof(string).GetTypeInfo(),
            Filters =
                {
                    Mock.Of<IFilterMetadata>(),
                    Mock.Of<IFilterMetadata>(),
                },
            HandlerMethods =
                {
                    new PageHandlerModel(handlerTypeInfo.GetMethod(nameof(TestModel.OnGet)), new object[0]),
                },
            HandlerProperties =
                {
                    new PagePropertyModel(handlerTypeInfo.GetProperty(nameof(TestModel.Property)), new object[0])
                    {
                        BindingInfo = new BindingInfo(),
                    },
                }
        };
        var globalFilters = new FilterCollection();

        // Act & Assert
        var actual = Assert.Throws<InvalidOperationException>(() =>
            CompiledPageActionDescriptorBuilder.Build(pageApplicationModel, globalFilters));
    }

    [Fact]
    public void CreateDescriptor_AddsGlobalFiltersWithTheRightScope()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor
        {
            ActionConstraints = new List<IActionConstraintMetadata>(),
            AttributeRouteInfo = new AttributeRouteInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            RelativePath = "/Foo",
            RouteValues = new Dictionary<string, string>(),
            ViewEnginePath = "/Pages/Foo",
        };
        var handlerTypeInfo = typeof(TestModel).GetTypeInfo();
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0])
        {
            PageType = typeof(TestPage).GetTypeInfo(),
            ModelType = typeof(TestModel).GetTypeInfo(),
            Filters =
                {
                    Mock.Of<IFilterMetadata>(),
                },
        };
        var globalFilters = new FilterCollection
            {
                Mock.Of<IFilterMetadata>(),
            };

        // Act
        var compiledPageActionDescriptor = CompiledPageActionDescriptorBuilder.Build(pageApplicationModel, globalFilters);

        // Assert
        Assert.Collection(
            compiledPageActionDescriptor.FilterDescriptors,
            filterDescriptor =>
            {
                Assert.Same(globalFilters[0], filterDescriptor.Filter);
                Assert.Equal(FilterScope.Global, filterDescriptor.Scope);
            },
            filterDescriptor =>
            {
                Assert.Same(pageApplicationModel.Filters[0], filterDescriptor.Filter);
                Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
            });
    }

    private class TestPage
    {
        public TestModel Model { get; } = new TestModel();

        [BindProperty]
        public string Property { get; set; }

        public void OnGet()
        {

        }
    }

    private class TestModel
    {
        [BindProperty]
        public string Property { get; set; }

        public void OnGet()
        {

        }
    }

    [Fact]
    public void CreateHandlerMethods_CopiesPropertiesFromHandlerModel()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor();
        var handlerTypeInfo = typeof(ModelWithHandler).GetTypeInfo();
        var handlerModel = new PageHandlerModel(handlerTypeInfo.GetMethod(nameof(ModelWithHandler.OnGetCustomerAsync)), new object[0])
        {
            HttpMethod = "GET",
            HandlerName = "Customer",
        };
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0])
        {
            HandlerMethods =
                {
                    handlerModel,
                }
        };

        // Act
        var handlerDescriptors = CompiledPageActionDescriptorBuilder.CreateHandlerMethods(pageApplicationModel);

        // Assert
        Assert.Collection(
            handlerDescriptors,
            d =>
            {
                Assert.Equal(handlerModel.MethodInfo, d.MethodInfo);
                Assert.Equal(handlerModel.HttpMethod, d.HttpMethod);
                Assert.Equal(handlerModel.HandlerName, d.Name);
            });
    }

    private class ModelWithHandler
    {
        public void OnGetCustomerAsync()
        {
        }
    }

    [Fact]
    public void CreateHandlerMethods_CopiesParameterDescriptorsFromParameterModel()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor();
        var handlerTypeInfo = typeof(HandlerWithParameters).GetTypeInfo();
        var handlerMethod = handlerTypeInfo.GetMethod(nameof(HandlerWithParameters.OnPost));
        var parameters = handlerMethod.GetParameters();
        var parameterModel1 = new PageParameterModel(parameters[0], new object[0])
        {
            ParameterName = "test-name"
        };
        var parameterModel2 = new PageParameterModel(parameters[1], new object[0])
        {
            BindingInfo = new BindingInfo(),
        };
        var handlerModel = new PageHandlerModel(handlerMethod, new object[0])
        {
            Parameters =
                {
                    parameterModel1,
                    parameterModel2,
                }
        };
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0])
        {
            HandlerMethods =
                {
                    handlerModel,
                }
        };

        // Act
        var handlerDescriptors = CompiledPageActionDescriptorBuilder.CreateHandlerMethods(pageApplicationModel);

        // Assert
        Assert.Collection(
            Assert.Single(handlerDescriptors).Parameters,
            p =>
            {
                Assert.Equal(parameters[0], p.ParameterInfo);
                Assert.Equal(typeof(string), p.ParameterType);
                Assert.Equal(parameterModel1.ParameterName, p.Name);
            },
            p =>
            {
                Assert.Equal(parameters[1], p.ParameterInfo);
                Assert.Equal(typeof(int), p.ParameterType);
                Assert.Same(parameterModel2.BindingInfo, p.BindingInfo);
            });
    }

    private class HandlerWithParameters
    {
        public void OnPost(string param1, [FromRoute(Name = "id")] int param2)
        {
        }
    }

    [Fact]
    public void CreateBoundProperties_CopiesPropertyDescriptorsFromPagePropertyModel()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor();
        var handlerTypeInfo = typeof(HandlerWithProperty).GetTypeInfo();
        var propertyModel = new PagePropertyModel(
            handlerTypeInfo.GetProperty(nameof(HandlerWithProperty.Property)),
            new object[0])
        {
            PropertyName = nameof(HandlerWithProperty.Property),
            BindingInfo = new BindingInfo(),
        };
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0])
        {
            HandlerProperties =
                {
                    propertyModel,
                }
        };

        // Act
        var propertyDescriptors = CompiledPageActionDescriptorBuilder.CreateBoundProperties(pageApplicationModel);

        // Assert
        Assert.Collection(
            propertyDescriptors,
            p =>
            {
                Assert.Same(propertyModel.PropertyName, p.Name);
                Assert.Same(typeof(int), p.ParameterType);
                Assert.Same(propertyModel.PropertyInfo, p.Property);
                Assert.Same(propertyModel.BindingInfo, p.BindingInfo);
            });
    }

    private class HandlerWithProperty
    {
        [BindProperty]
        public int Property { get; set; }
    }

    [Fact]
    public void CreateBoundProperties_IgnoresPropertiesWithoutBindingInfo()
    {
        // Arrange
        var actionDescriptor = new PageActionDescriptor();
        var handlerTypeInfo = typeof(HandlerWithIgnoredProperties).GetTypeInfo();
        var propertyModel1 = new PagePropertyModel(
            handlerTypeInfo.GetProperty(nameof(HandlerWithIgnoredProperties.Property)),
            new object[0])
        {
            PropertyName = nameof(HandlerWithIgnoredProperties.Property),
            BindingInfo = new BindingInfo(),
        };
        var propertyModel2 = new PagePropertyModel(
            handlerTypeInfo.GetProperty(nameof(HandlerWithIgnoredProperties.IgnoreMe)),
            new object[0])
        {
            PropertyName = nameof(HandlerWithIgnoredProperties.IgnoreMe),
        };
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new object[0])
        {
            HandlerProperties =
                {
                    propertyModel1,
                    propertyModel2,
                }
        };

        // Act
        var propertyDescriptors = CompiledPageActionDescriptorBuilder.CreateBoundProperties(pageApplicationModel);

        // Assert
        Assert.Collection(
            propertyDescriptors,
            p =>
            {
                Assert.Same(propertyModel1.PropertyName, p.Name);
                Assert.Same(typeof(int), p.ParameterType);
                Assert.Same(propertyModel1.PropertyInfo, p.Property);
                Assert.Same(propertyModel1.BindingInfo, p.BindingInfo);
            });
    }

    [Fact]
    public void CreateDescriptor_CombinesEndpointMetadataFromHandlerTypeAttributesAndAttributesOnModel()
    {
        // Arrange
        var metadata1 = "metadata1";
        var metadata2 = "metadata2";
        var metadata3 = "metadata3";
        var metadata4 = "metadata4";
        var metadata5 = "metadata5";
        var metadata6 = "metadata6";

        var actionDescriptor = new PageActionDescriptor
        {
            ActionConstraints = new List<IActionConstraintMetadata>(),
            AttributeRouteInfo = new AttributeRouteInfo(),
            EndpointMetadata = new List<object> { metadata3, metadata4, },
            FilterDescriptors = new List<FilterDescriptor>(),
            RelativePath = "/Foo",
            RouteValues = new Dictionary<string, string>(),
            ViewEnginePath = "/Pages/Foo",
        };
        var handlerTypeInfo = typeof(object).GetTypeInfo();
        var pageApplicationModel = new PageApplicationModel(actionDescriptor, handlerTypeInfo, new[] { metadata1, metadata2, });
        pageApplicationModel.EndpointMetadata.Add(metadata5);
        pageApplicationModel.EndpointMetadata.Add(metadata6);
        var globalFilters = new FilterCollection();

        // Act
        var actual = CompiledPageActionDescriptorBuilder.Build(pageApplicationModel, globalFilters);

        // Assert
        Assert.Equal(new[] { metadata1, metadata2, metadata3, metadata4, metadata5, metadata6 }, actual.EndpointMetadata);
    }

    private class HandlerWithIgnoredProperties
    {
        [BindProperty]
        public int Property { get; set; }

        public string IgnoreMe { get; set; }
    }
}
