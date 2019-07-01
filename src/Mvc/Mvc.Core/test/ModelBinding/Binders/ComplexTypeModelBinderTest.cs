// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ComplexTypeModelBinderTest
    {
        private static readonly IModelMetadataProvider _metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        [Theory]
        [InlineData(true, ComplexTypeModelBinder.ValueProviderDataAvailable)]
        [InlineData(false, ComplexTypeModelBinder.NoDataAvailable)]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObject(bool isTopLevelObject, int expectedCanCreate)
        {
            var bindingContext = CreateContext(GetMetadataForType(typeof(Person)));
            bindingContext.IsTopLevelObject = isTopLevelObject;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsFalse_IfNotIsTopLevelObjectAndModelIsMarkedWithBinderMetadata()
        {
            var modelMetadata = GetMetadataForProperty(typeof(Document), nameof(Document.SubDocument));

            var bindingContext = CreateContext(modelMetadata);
            bindingContext.IsTopLevelObject = false;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(ComplexTypeModelBinder.NoDataAvailable, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObjectAndModelIsMarkedWithBinderMetadata()
        {
            var bindingContext = CreateContext(GetMetadataForType(typeof(Document)));
            bindingContext.IsTopLevelObject = true;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(ComplexTypeModelBinder.ValueProviderDataAvailable, canCreate);
        }

        [Theory]
        [InlineData(ComplexTypeModelBinder.ValueProviderDataAvailable)]
        [InlineData(ComplexTypeModelBinder.GreedyPropertiesMayHaveData)]
        public void CanCreateModel_CreatesModel_WithAllGreedyProperties(int expectedCanCreate)
        {
            var bindingContext = CreateContext(GetMetadataForType(typeof(HasAllGreedyProperties)));
            bindingContext.IsTopLevelObject = expectedCanCreate == ComplexTypeModelBinder.ValueProviderDataAvailable;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        [Theory]
        [InlineData(ComplexTypeModelBinder.ValueProviderDataAvailable)]
        [InlineData(ComplexTypeModelBinder.NoDataAvailable)]
        public void CanCreateModel_ReturnsTrue_IfNotIsTopLevelObject_BasedOnValueAvailability(int valueAvailable)
        {
            // Arrange
            var valueProvider = new Mock<IValueProvider>(MockBehavior.Strict);
            valueProvider
                .Setup(provider => provider.ContainsPrefix("SimpleContainer.Simple.Name"))
                .Returns(valueAvailable == ComplexTypeModelBinder.ValueProviderDataAvailable);

            var modelMetadata = GetMetadataForProperty(typeof(SimpleContainer), nameof(SimpleContainer.Simple));
            var bindingContext = CreateContext(modelMetadata);
            bindingContext.IsTopLevelObject = false;
            bindingContext.ModelName = "SimpleContainer.Simple";
            bindingContext.ValueProvider = valueProvider.Object;
            bindingContext.OriginalValueProvider = valueProvider.Object;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            // Result matches whether first Simple property can bind.
            Assert.Equal(valueAvailable, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsFalse_IfNotIsTopLevelObjectAndModelHasNoProperties()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForType(typeof(PersonWithNoProperties)));
            bindingContext.IsTopLevelObject = false;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(ComplexTypeModelBinder.NoDataAvailable, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObjectAndModelHasNoProperties()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForType(typeof(PersonWithNoProperties)));
            bindingContext.IsTopLevelObject = true;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(ComplexTypeModelBinder.ValueProviderDataAvailable, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithNoBinderMetadata), ComplexTypeModelBinder.NoDataAvailable)]
        [InlineData(typeof(TypeWithNoBinderMetadata), ComplexTypeModelBinder.ValueProviderDataAvailable)]
        public void CanCreateModel_CreatesModelForValueProviderBasedBinderMetadatas_IfAValueProviderProvidesValue(
            Type modelType,
            int valueProviderProvidesValue)
        {
            var valueProvider = new Mock<IValueProvider>();
            valueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(valueProviderProvidesValue == ComplexTypeModelBinder.ValueProviderDataAvailable);

            var bindingContext = CreateContext(GetMetadataForType(modelType));
            bindingContext.IsTopLevelObject = false;
            bindingContext.ValueProvider = valueProvider.Object;
            bindingContext.OriginalValueProvider = valueProvider.Object;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(valueProviderProvidesValue, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), ComplexTypeModelBinder.GreedyPropertiesMayHaveData)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), ComplexTypeModelBinder.ValueProviderDataAvailable)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), ComplexTypeModelBinder.GreedyPropertiesMayHaveData)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), ComplexTypeModelBinder.ValueProviderDataAvailable)]
        public void CanCreateModel_CreatesModelForValueProviderBasedBinderMetadatas_IfPropertyHasGreedyBindingSource(
            Type modelType,
            int expectedCanCreate)
        {
            var valueProvider = new Mock<IValueProvider>();
            valueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(expectedCanCreate == ComplexTypeModelBinder.ValueProviderDataAvailable);

            var bindingContext = CreateContext(GetMetadataForType(modelType));
            bindingContext.IsTopLevelObject = false;
            bindingContext.ValueProvider = valueProvider.Object;
            bindingContext.OriginalValueProvider = valueProvider.Object;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), ComplexTypeModelBinder.GreedyPropertiesMayHaveData)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), ComplexTypeModelBinder.ValueProviderDataAvailable)]
        public void CanCreateModel_ForExplicitValueProviderMetadata_UsesOriginalValueProvider(
            Type modelType,
            int expectedCanCreate)
        {
            var valueProvider = new Mock<IValueProvider>();
            valueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            var originalValueProvider = new Mock<IBindingSourceValueProvider>();
            originalValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(expectedCanCreate == ComplexTypeModelBinder.ValueProviderDataAvailable);

            originalValueProvider
                .Setup(o => o.Filter(It.IsAny<BindingSource>()))
                .Returns<BindingSource>(source => source == BindingSource.Query ? originalValueProvider.Object : null);

            var bindingContext = CreateContext(GetMetadataForType(modelType));
            bindingContext.IsTopLevelObject = false;
            bindingContext.ValueProvider = valueProvider.Object;
            bindingContext.OriginalValueProvider = originalValueProvider.Object;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), false, ComplexTypeModelBinder.GreedyPropertiesMayHaveData)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), true, ComplexTypeModelBinder.ValueProviderDataAvailable)]
        [InlineData(typeof(TypeWithNoBinderMetadata), false, ComplexTypeModelBinder.NoDataAvailable)]
        [InlineData(typeof(TypeWithNoBinderMetadata), true, ComplexTypeModelBinder.ValueProviderDataAvailable)]
        public void CanCreateModel_UnmarkedProperties_UsesCurrentValueProvider(
            Type modelType,
            bool valueProviderProvidesValue,
            int expectedCanCreate)
        {
            var valueProvider = new Mock<IValueProvider>();
            valueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(valueProviderProvidesValue);

            var originalValueProvider = new Mock<IValueProvider>();
            originalValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            var bindingContext = CreateContext(GetMetadataForType(modelType));
            bindingContext.IsTopLevelObject = false;
            bindingContext.ValueProvider = valueProvider.Object;
            bindingContext.OriginalValueProvider = originalValueProvider.Object;

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var canCreate = binder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        private IActionResult ActionWithComplexParameter(Person parameter) => null;

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task BindModelAsync_CreatesModel_IfIsTopLevelObject(
            bool allowValidatingTopLevelNodes,
            bool isBindingRequired)
        {
            // Arrange
            var expectedErrorCount = isBindingRequired ? 1 : 0;
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            // Mock binder fails to bind all properties.
            var mockBinder = new StubModelBinder();

            var parameter = typeof(ComplexTypeModelBinderTest)
                .GetMethod(nameof(ActionWithComplexParameter), BindingFlags.Instance | BindingFlags.NonPublic)
                .GetParameters()[0];
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForParameter(parameter)
                .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
            var metadata = metadataProvider.GetMetadataForParameter(parameter);
            var bindingContext = new DefaultModelBindingContext
            {
                IsTopLevelObject = true,
                ModelMetadata = metadata,
                ModelName = string.Empty,
                ValueProvider = mockValueProvider.Object,
                ModelState = new ModelStateDictionary(),
            };

            var model = new Person();

            var testableBinder = new Mock<TestableComplexTypeModelBinder>(allowValidatingTopLevelNodes)
            {
                CallBase = true
            };
            testableBinder
                .Setup(o => o.CreateModelPublic(bindingContext))
                .Returns(model)
                .Verifiable();
            testableBinder
                .Setup(o => o.CanBindPropertyPublic(bindingContext, It.IsAny<ModelMetadata>()))
                .Returns(false);

            // Act
            await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.Equal(expectedErrorCount, bindingContext.ModelState.ErrorCount);

            var returnedPerson = Assert.IsType<Person>(bindingContext.Result.Model);
            Assert.Same(model, returnedPerson);
            testableBinder.Verify();
        }

        [Fact]
        public async Task BindModelAsync_CreatesModelAndAddsError_IfIsTopLevelObject_WithNoData()
        {
            // Arrange
            var parameter = typeof(ComplexTypeModelBinderTest)
                .GetMethod(nameof(ActionWithComplexParameter), BindingFlags.Instance | BindingFlags.NonPublic)
                .GetParameters()[0];
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForParameter(parameter)
                .BindingDetails(b => b.IsBindingRequired = true);
            var metadata = metadataProvider.GetMetadataForParameter(parameter);
            var bindingContext = new DefaultModelBindingContext
            {
                IsTopLevelObject = true,
                FieldName = "fieldName",
                ModelMetadata = metadata,
                ModelName = string.Empty,
                ValueProvider = new TestValueProvider(new Dictionary<string, object>()),
                ModelState = new ModelStateDictionary(),
            };

            // Mock binder fails to bind all properties.
            var innerBinder = new StubModelBinder();
            var binders = new Dictionary<ModelMetadata, IModelBinder>();
            foreach (var property in metadataProvider.GetMetadataForProperties(typeof(Person)))
            {
                binders.Add(property, innerBinder);
            }

            var binder = new ComplexTypeModelBinder(
                binders,
                NullLoggerFactory.Instance,
                allowValidatingTopLevelNodes: true);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType<Person>(bindingContext.Result.Model);

            var keyValuePair = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, keyValuePair.Key);
            var error = Assert.Single(keyValuePair.Value.Errors);
            Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
        }

        private IActionResult ActionWithNoSettablePropertiesParameter(PersonWithNoProperties parameter) => null;

        [Fact]
        public async Task BindModelAsync_CreatesModelAndAddsError_IfIsTopLevelObject_WithNoSettableProperties()
        {
            // Arrange
            var parameter = typeof(ComplexTypeModelBinderTest)
                .GetMethod(
                    nameof(ActionWithNoSettablePropertiesParameter),
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetParameters()[0];
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForParameter(parameter)
                .BindingDetails(b => b.IsBindingRequired = true);
            var metadata = metadataProvider.GetMetadataForParameter(parameter);
            var bindingContext = new DefaultModelBindingContext
            {
                IsTopLevelObject = true,
                FieldName = "fieldName",
                ModelMetadata = metadata,
                ModelName = string.Empty,
                ValueProvider = new TestValueProvider(new Dictionary<string, object>()),
                ModelState = new ModelStateDictionary(),
            };

            var binder = new ComplexTypeModelBinder(
                new Dictionary<ModelMetadata, IModelBinder>(),
                NullLoggerFactory.Instance,
                allowValidatingTopLevelNodes: true);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType<PersonWithNoProperties>(bindingContext.Result.Model);

            var keyValuePair = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, keyValuePair.Key);
            var error = Assert.Single(keyValuePair.Value.Errors);
            Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
        }

        private IActionResult ActionWithAllPropertiesExcludedParameter(PersonWithAllPropertiesExcluded parameter) => null;

        [Fact]
        public async Task BindModelAsync_CreatesModelAndAddsError_IfIsTopLevelObject_WithAllPropertiesExcluded()
        {
            // Arrange
            var parameter = typeof(ComplexTypeModelBinderTest)
                .GetMethod(
                    nameof(ActionWithAllPropertiesExcludedParameter),
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetParameters()[0];
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForParameter(parameter)
                .BindingDetails(b => b.IsBindingRequired = true);
            var metadata = metadataProvider.GetMetadataForParameter(parameter);
            var bindingContext = new DefaultModelBindingContext
            {
                IsTopLevelObject = true,
                FieldName = "fieldName",
                ModelMetadata = metadata,
                ModelName = string.Empty,
                ValueProvider = new TestValueProvider(new Dictionary<string, object>()),
                ModelState = new ModelStateDictionary(),
            };

            var binder = new ComplexTypeModelBinder(
                new Dictionary<ModelMetadata, IModelBinder>(),
                NullLoggerFactory.Instance,
                allowValidatingTopLevelNodes: true);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);
            Assert.IsType<PersonWithAllPropertiesExcluded>(bindingContext.Result.Model);

            var keyValuePair = Assert.Single(bindingContext.ModelState);
            Assert.Equal(string.Empty, keyValuePair.Key);
            var error = Assert.Single(keyValuePair.Value.Errors);
            Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
        }

        [Theory]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyInt), false)]    // read-only value type
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyObject), true)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlySimple), true)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyString), false)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadWriteString), true)]
        public void CanUpdateProperty_ReturnsExpectedValue(string propertyName, bool expected)
        {
            // Arrange

            var propertyMetadata = GetMetadataForProperty(typeof(MyModelTestingCanUpdateProperty), propertyName);

            // Act
            var canUpdate = ComplexTypeModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.Equal(expected, canUpdate);
        }

        [Theory]
        [InlineData(nameof(CollectionContainer.ReadOnlyArray), false)]
        [InlineData(nameof(CollectionContainer.ReadOnlyDictionary), true)]
        [InlineData(nameof(CollectionContainer.ReadOnlyList), true)]
        [InlineData(nameof(CollectionContainer.SettableArray), true)]
        [InlineData(nameof(CollectionContainer.SettableDictionary), true)]
        [InlineData(nameof(CollectionContainer.SettableList), true)]
        public void CanUpdateProperty_CollectionProperty_FalseOnlyForArray(string propertyName, bool expected)
        {
            // Arrange
            var metadataProvider = _metadataProvider;
            var metadata = metadataProvider.GetMetadataForProperty(typeof(CollectionContainer), propertyName);

            // Act
            var canUpdate = ComplexTypeModelBinder.CanUpdatePropertyInternal(metadata);

            // Assert
            Assert.Equal(expected, canUpdate);
        }

        [Fact]
        public void CreateModel_InstantiatesInstanceOfMetadataType()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var model = binder.CreateModelPublic(bindingContext);

            // Assert
            Assert.IsType<Person>(model);
        }

        [Fact]
        public void CreateModel_ForStructModelType_AsTopLevelObject_ThrowsException()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(PointStruct)),
                IsTopLevelObject = true
            };
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => binder.CreateModelPublic(bindingContext));
            Assert.Equal(
                string.Format(
                    "Could not create an instance of type '{0}'. Model bound complex types must not be abstract or " +
                    "value types and must have a parameterless constructor.",
                    typeof(PointStruct).FullName),
                exception.Message);
        }

        [Fact]
        public void CreateModel_ForClassWithNoParameterlessConstructor_AsElement_ThrowsException()
        {
            // Arrange
            var expectedMessage = "Could not create an instance of type " +
                $"'{typeof(ClassWithNoParameterlessConstructor)}'. Model bound complex types must not be abstract " +
                "or value types and must have a parameterless constructor.";
            var metadata = GetMetadataForType(typeof(ClassWithNoParameterlessConstructor));
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadata,
            };
            var binder = CreateBinder(metadata);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => binder.CreateModelPublic(bindingContext));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void CreateModel_ForStructModelType_AsProperty_ThrowsException()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = GetMetadataForProperty(typeof(Location), nameof(Location.Point)),
                ModelName = nameof(Location.Point),
                IsTopLevelObject = false
            };
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => binder.CreateModelPublic(bindingContext));
            Assert.Equal(
                string.Format(
                    "Could not create an instance of type '{0}'. Model bound complex types must not be abstract or " +
                    "value types and must have a parameterless constructor. Alternatively, set the '{1}' property to" +
                    " a non-null value in the '{2}' constructor.",
                    typeof(PointStruct).FullName,
                    nameof(Location.Point),
                    typeof(Location).FullName),
                exception.Message);
        }

        [Fact]
        public async Task BindModelAsync_ModelIsNotNull_DoesNotCallCreateModel()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForType(typeof(Person)), new Person());
            var originalModel = bindingContext.Model;
            var binders = bindingContext.ModelMetadata.Properties.ToDictionary(
                keySelector: item => item,
                elementSelector: item => (IModelBinder)null);

            var binder = new Mock<TestableComplexTypeModelBinder>(binders) { CallBase = true };
            binder
                .Setup(b => b.CreateModelPublic(It.IsAny<ModelBindingContext>()))
                .Verifiable();

            // Act
            await binder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.Same(originalModel, bindingContext.Model);
            binder.Verify(o => o.CreateModelPublic(bindingContext), Times.Never());
        }

        [Fact]
        public async Task BindModelAsync_ModelIsNull_CallsCreateModel()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForType(typeof(Person)), model: null);
            var binders = bindingContext.ModelMetadata.Properties.ToDictionary(
                keySelector: item => item,
                elementSelector: item => (IModelBinder)null);

            var testableBinder = new Mock<TestableComplexTypeModelBinder>(binders) { CallBase = true };
            testableBinder
                .Setup(o => o.CreateModelPublic(bindingContext))
                .Returns(new Person())
                .Verifiable();

            // Act
            await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(bindingContext.Model);
            Assert.IsType<Person>(bindingContext.Model);
            testableBinder.Verify();
        }

        [Theory]
        [InlineData(nameof(PersonWithBindExclusion.FirstName))]
        [InlineData(nameof(PersonWithBindExclusion.LastName))]
        public void CanBindProperty_GetSetProperty(string property)
        {
            // Arrange
            var metadata = GetMetadataForProperty(typeof(PersonWithBindExclusion), property);
            var bindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = new ServiceCollection().BuildServiceProvider(),
                    },
                },
                ModelMetadata = GetMetadataForType(typeof(PersonWithBindExclusion)),
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var result = binder.CanBindPropertyPublic(bindingContext, metadata);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(PersonWithBindExclusion.NonUpdateableProperty))]
        public void CanBindProperty_GetOnlyProperty_WithBindNever(string property)
        {
            // Arrange
            var metadata = GetMetadataForProperty(typeof(PersonWithBindExclusion), property);
            var bindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        RequestServices = new ServiceCollection().BuildServiceProvider(),
                    },
                },
                ModelMetadata = GetMetadataForType(typeof(PersonWithBindExclusion)),
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var result = binder.CanBindPropertyPublic(bindingContext, metadata);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(nameof(PersonWithBindExclusion.DateOfBirth))]
        [InlineData(nameof(PersonWithBindExclusion.DateOfDeath))]
        public void CanBindProperty_GetSetProperty_WithBindNever(string property)
        {
            // Arrange
            var metadata = GetMetadataForProperty(typeof(PersonWithBindExclusion), property);
            var bindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
                ModelMetadata = GetMetadataForType(typeof(PersonWithBindExclusion)),
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var result = binder.CanBindPropertyPublic(bindingContext, metadata);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(nameof(TypeWithIncludedPropertiesUsingBindAttribute.IncludedExplicitly1), true)]
        [InlineData(nameof(TypeWithIncludedPropertiesUsingBindAttribute.IncludedExplicitly2), true)]
        [InlineData(nameof(TypeWithIncludedPropertiesUsingBindAttribute.ExcludedByDefault1), false)]
        [InlineData(nameof(TypeWithIncludedPropertiesUsingBindAttribute.ExcludedByDefault2), false)]
        public void CanBindProperty_WithBindInclude(string property, bool expected)
        {
            // Arrange
            var metadata = GetMetadataForProperty(typeof(TypeWithIncludedPropertiesUsingBindAttribute), property);
            var bindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext()
                },
                ModelMetadata = GetMetadataForType(typeof(TypeWithIncludedPropertiesUsingBindAttribute)),
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var result = binder.CanBindPropertyPublic(bindingContext, metadata);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(nameof(ModelWithMixedBindingBehaviors.Required), true)]
        [InlineData(nameof(ModelWithMixedBindingBehaviors.Optional), true)]
        [InlineData(nameof(ModelWithMixedBindingBehaviors.Never), false)]
        public void CanBindProperty_BindingAttributes_OverridingBehavior(string property, bool expected)
        {
            // Arrange
            var metadata = GetMetadataForProperty(typeof(ModelWithMixedBindingBehaviors), property);
            var bindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
                ModelMetadata = GetMetadataForType(typeof(ModelWithMixedBindingBehaviors)),
            };

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            var result = binder.CanBindPropertyPublic(bindingContext, metadata);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        [ReplaceCulture]
        public async Task BindModelAsync_BindRequiredFieldMissing_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithBindRequired
            {
                Name = "original value",
                Age = -20
            };

            var property = GetMetadataForProperty(model.GetType(), nameof(ModelWithBindRequired.Age));

            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var binder = CreateBinder(bindingContext.ModelMetadata);
            binder.Results[property] = ModelBindingResult.Failed();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Single(modelStateDictionary);

            // Check Age error.
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out var entry));
            var modelError = Assert.Single(entry.Errors);
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("A value for the 'Age' parameter or property was not provided.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public async Task BindModelAsync_DataMemberIsRequiredFieldMissing_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithDataMemberIsRequired
            {
                Name = "original value",
                Age = -20
            };

            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            var property = GetMetadataForProperty(model.GetType(), nameof(ModelWithDataMemberIsRequired.Age));
            binder.Results[property] = ModelBindingResult.Failed();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Single(modelStateDictionary);

            // Check Age error.
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out var entry));
            var modelError = Assert.Single(entry.Errors);
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("A value for the 'Age' parameter or property was not provided.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public async Task BindModelAsync_ValueTypePropertyWithBindRequired_SetToNull_CapturesException()
        {
            // Arrange
            var model = new ModelWithBindRequired
            {
                Name = "original value",
                Age = -20
            };

            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Attempt to set non-Nullable property to null. BindRequiredAttribute should not be relevant in this
            // case because the property did have a result.
            var property = GetMetadataForProperty(model.GetType(), nameof(ModelWithBindRequired.Age));
            binder.Results[property] = ModelBindingResult.Success(model: null);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Single(modelStateDictionary);

            // Check Age error.
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out var entry));
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            var modelError = Assert.Single(entry.Errors);
            Assert.Equal(string.Empty, modelError.ErrorMessage);
            Assert.IsType<NullReferenceException>(modelError.Exception);
        }

        [Fact]
        public async Task BindModelAsync_ValueTypeProperty_WithBindingOptional_NoValueSet_NoError()
        {
            // Arrange
            var model = new BindingOptionalProperty();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            var property = GetMetadataForProperty(model.GetType(), nameof(BindingOptionalProperty.ValueTypeRequired));
            binder.Results[property] = ModelBindingResult.Failed();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
        }

        [Fact]
        public async Task BindModelAsync_NullableValueTypeProperty_NoValueSet_NoError()
        {
            // Arrange
            var model = new NullableValueTypeProperty();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            var property = GetMetadataForProperty(model.GetType(), nameof(NullableValueTypeProperty.NullableValueType));
            binder.Results[property] = ModelBindingResult.Failed();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
        }

        [Fact]
        public async Task BindModelAsync_ValueTypeProperty_NoValue_NoError()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            var property = GetMetadataForProperty(model.GetType(), nameof(Person.ValueTypeRequired));
            binder.Results[property] = ModelBindingResult.Failed();

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(0, model.ValueTypeRequired);
        }

        [Fact]
        public async Task BindModelAsync_ProvideRequiredField_Success()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            var property = GetMetadataForProperty(model.GetType(), nameof(Person.ValueTypeRequired));
            binder.Results[property] = ModelBindingResult.Success(model: 57);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(57, model.ValueTypeRequired);
        }

        [Fact]
        public async Task BindModelAsync_Success()
        {
            // Arrange
            var dob = new DateTime(2001, 1, 1);
            var model = new PersonWithBindExclusion
            {
                DateOfBirth = dob
            };

            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);

            var binder = CreateBinder(bindingContext.ModelMetadata);

            foreach (var property in containerMetadata.Properties)
            {
                binder.Results[property] = ModelBindingResult.Failed();
            }

            var firstNameProperty = containerMetadata.Properties[nameof(model.FirstName)];
            binder.Results[firstNameProperty] = ModelBindingResult.Success("John");

            var lastNameProperty = containerMetadata.Properties[nameof(model.LastName)];
            binder.Results[lastNameProperty] = ModelBindingResult.Success("Doe");

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal("John", model.FirstName);
            Assert.Equal("Doe", model.LastName);
            Assert.Equal(dob, model.DateOfBirth);
            Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyHasDefaultValue_DefaultValueAttributeDoesNothing()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadata = GetMetadataForType(typeof(Person));
            var propertyMetadata = metadata.Properties[nameof(model.PropertyWithDefaultValue)];

            var result = ModelBindingResult.Failed();
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            var person = Assert.IsType<Person>(bindingContext.Model);
            Assert.Equal(0m, person.PropertyWithDefaultValue);
            Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyIsPreinitialized_NoValue_DoesNothing()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadata = GetMetadataForType(typeof(Person));
            var propertyMetadata = metadata.Properties[nameof(model.PropertyWithInitializedValue)];

            // The null model value won't be used because IsModelBound = false.
            var result = ModelBindingResult.Failed();

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            var person = Assert.IsType<Person>(bindingContext.Model);
            Assert.Equal("preinitialized", person.PropertyWithInitializedValue);
            Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyIsPreinitialized_DefaultValueAttributeDoesNothing()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadata = GetMetadataForType(typeof(Person));
            var propertyMetadata = metadata.Properties[nameof(model.PropertyWithInitializedValueAndDefault)];

            // The null model value won't be used because IsModelBound = false.
            var result = ModelBindingResult.Failed();

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            var person = Assert.IsType<Person>(bindingContext.Model);
            Assert.Equal("preinitialized", person.PropertyWithInitializedValueAndDefault);
            Assert.True(bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyIsReadOnly_DoesNothing()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadata = GetMetadataForType(typeof(Person));
            var propertyMetadata = metadata.Properties[nameof(model.NonUpdateableProperty)];

            var result = ModelBindingResult.Failed();
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            // If didn't throw, success!
        }

        // Property name, property accessor
        public static TheoryData<string, Func<object, object>> MyCanUpdateButCannotSetPropertyData
        {
            get
            {
                return new TheoryData<string, Func<object, object>>
                {
                    {
                        nameof(MyModelTestingCanUpdateProperty.ReadOnlyObject),
                        model => ((Simple)((MyModelTestingCanUpdateProperty)model).ReadOnlyObject).Name
                    },
                    {
                        nameof(MyModelTestingCanUpdateProperty.ReadOnlySimple),
                        model => ((MyModelTestingCanUpdateProperty)model).ReadOnlySimple.Name
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MyCanUpdateButCannotSetPropertyData))]
        public void SetProperty_ValueProvidedAndCanUpdatePropertyTrue_DoesNothing(
            string propertyName,
            Func<object, object> propertyAccessor)
        {
            // Arrange
            var model = new MyModelTestingCanUpdateProperty();
            var type = model.GetType();
            var bindingContext = CreateContext(GetMetadataForType(type), model);
            var modelState = bindingContext.ModelState;
            var propertyMetadata = bindingContext.ModelMetadata.Properties[propertyName];
            var result = ModelBindingResult.Success(new Simple { Name = "Hanna" });

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, propertyName, propertyMetadata, result);

            // Assert
            Assert.Equal("Joe", propertyAccessor(model));
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public void SetProperty_ReadOnlyProperty_IsNoOp()
        {
            // Arrange
            var model = new CollectionContainer();
            var originalCollection = model.ReadOnlyList;

            var modelMetadata = GetMetadataForType(model.GetType());
            var propertyMetadata = GetMetadataForProperty(model.GetType(), nameof(CollectionContainer.ReadOnlyList));

            var bindingContext = CreateContext(modelMetadata, model);
            var result = ModelBindingResult.Success(new List<string>() { "hi" });

            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, propertyMetadata.PropertyName, propertyMetadata, result);

            // Assert
            Assert.Same(originalCollection, model.ReadOnlyList);
            Assert.Empty(model.ReadOnlyList);
        }

        [Fact]
        public void SetProperty_PropertyIsSettable_CallsSetter()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.DateOfBirth)];

            var result = ModelBindingResult.Success(new DateTime(2001, 1, 1));
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(new DateTime(2001, 1, 1), model.DateOfBirth);
        }

        [Fact]
        [ReplaceCulture]
        public void SetProperty_PropertyIsSettable_SetterThrows_RecordsError()
        {
            // Arrange
            var model = new Person
            {
                DateOfBirth = new DateTime(1900, 1, 1)
            };

            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.DateOfDeath)];

            var result = ModelBindingResult.Success(new DateTime(1800, 1, 1));
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo", propertyMetadata, result);

            // Assert
            Assert.Equal("Date of death can't be before date of birth. (Parameter 'value')",
                         bindingContext.ModelState["foo"].Errors[0].Exception.Message);
        }

        [Fact]
        [ReplaceCulture]
        public void SetProperty_PropertySetterThrows_CapturesException()
        {
            // Arrange
            var model = new ModelWhosePropertySetterThrows();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);
            bindingContext.ModelName = "foo";
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.NameNoAttribute)];

            var result = ModelBindingResult.Success(model: null);
            var binder = CreateBinder(bindingContext.ModelMetadata);

            // Act
            binder.SetPropertyPublic(bindingContext, "foo.NameNoAttribute", propertyMetadata, result);

            // Assert
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Single(bindingContext.ModelState["foo.NameNoAttribute"].Errors);
            Assert.Equal("This is a different exception. (Parameter 'value')",
                         bindingContext.ModelState["foo.NameNoAttribute"].Errors[0].Exception.Message);
        }

        private static TestableComplexTypeModelBinder CreateBinder(ModelMetadata metadata)
        {
            var options = Options.Create(new MvcOptions());
            var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
            setup.Configure(options.Value);

            var lastIndex = options.Value.ModelBinderProviders.Count - 1;
            Assert.IsType<ComplexTypeModelBinderProvider>(options.Value.ModelBinderProviders[lastIndex]);
            options.Value.ModelBinderProviders.RemoveAt(lastIndex);
            options.Value.ModelBinderProviders.Add(new TestableComplexTypeModelBinderProvider());

            var factory = TestModelBinderFactory.Create(options.Value.ModelBinderProviders.ToArray());
            return (TestableComplexTypeModelBinder)factory.CreateBinder(new ModelBinderFactoryContext()
            {
                Metadata = metadata,
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = metadata.BinderModelName,
                    BinderType = metadata.BinderType,
                    BindingSource = metadata.BindingSource,
                    PropertyFilterProvider = metadata.PropertyFilterProvider,
                },
            });
        }

        private static DefaultModelBindingContext CreateContext(ModelMetadata metadata, object model = null)
        {
            var valueProvider = new TestValueProvider(new Dictionary<string, object>());
            return new DefaultModelBindingContext()
            {
                BinderModelName = metadata.BinderModelName,
                BindingSource = metadata.BindingSource,
                IsTopLevelObject = true,
                Model = model,
                ModelMetadata = metadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
            };
        }

        private static ModelMetadata GetMetadataForType(Type type)
        {
            return _metadataProvider.GetMetadataForType(type);
        }

        private static ModelMetadata GetMetadataForProperty(Type type, string propertyName)
        {
            return _metadataProvider.GetMetadataForProperty(type, propertyName);
        }

        private class Location
        {
            public PointStruct Point { get; set; }
        }

        private readonly struct PointStruct
        {
            public PointStruct(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }

        private class ClassWithNoParameterlessConstructor
        {
            public ClassWithNoParameterlessConstructor(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
        }

        private class BindingOptionalProperty
        {
            [BindingBehavior(BindingBehavior.Optional)]
            public int ValueTypeRequired { get; set; }
        }

        private class NullableValueTypeProperty
        {
            [BindingBehavior(BindingBehavior.Optional)]
            public int? NullableValueType { get; set; }
        }

        private class Person
        {
            private DateTime? _dateOfDeath;

            [BindingBehavior(BindingBehavior.Optional)]
            public DateTime DateOfBirth { get; set; }

            public DateTime? DateOfDeath
            {
                get { return _dateOfDeath; }
                set
                {
                    if (value < DateOfBirth)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Date of death can't be before date of birth.");
                    }
                    _dateOfDeath = value;
                }
            }

            [Required(ErrorMessage = "Sample message")]
            public int ValueTypeRequired { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string NonUpdateableProperty { get; private set; }

            [BindingBehavior(BindingBehavior.Optional)]
            [DefaultValue(typeof(decimal), "123.456")]
            public decimal PropertyWithDefaultValue { get; set; }

            public string PropertyWithInitializedValue { get; set; } = "preinitialized";

            [DefaultValue("default")]
            public string PropertyWithInitializedValueAndDefault { get; set; } = "preinitialized";
        }

        private class PersonWithNoProperties
        {
            public string name = null;
        }

        private class PersonWithAllPropertiesExcluded
        {
            [BindNever]
            public DateTime DateOfBirth { get; set; }

            [BindNever]
            public DateTime? DateOfDeath { get; set; }

            [BindNever]
            public string FirstName { get; set; }

            [BindNever]
            public string LastName { get; set; }

            public string NonUpdateableProperty { get; private set; }
        }

        private class PersonWithBindExclusion
        {
            [BindNever]
            public DateTime DateOfBirth { get; set; }

            [BindNever]
            public DateTime? DateOfDeath { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string NonUpdateableProperty { get; private set; }
        }

        private class ModelWithBindRequired
        {
            public string Name { get; set; }

            [BindRequired]
            public int Age { get; set; }
        }

        [DataContract]
        private class ModelWithDataMemberIsRequired
        {
            public string Name { get; set; }

            [DataMember(IsRequired = true)]
            public int Age { get; set; }
        }

        [BindRequired]
        private class ModelWithMixedBindingBehaviors
        {
            public string Required { get; set; }

            [BindNever]
            public string Never { get; set; }

            [BindingBehavior(BindingBehavior.Optional)]
            public string Optional { get; set; }
        }

        private sealed class MyModelTestingCanUpdateProperty
        {
            public int ReadOnlyInt { get; private set; }
            public string ReadOnlyString { get; private set; }
            public object ReadOnlyObject { get; } = new Simple { Name = "Joe" };
            public string ReadWriteString { get; set; }
            public Simple ReadOnlySimple { get; } = new Simple { Name = "Joe" };
        }

        private sealed class ModelWhosePropertySetterThrows
        {
            [Required(ErrorMessage = "This message comes from the [Required] attribute.")]
            public string Name
            {
                get { return null; }
                set { throw new ArgumentException("This is an exception.", "value"); }
            }

            public string NameNoAttribute
            {
                get { return null; }
                set { throw new ArgumentException("This is a different exception.", "value"); }
            }
        }

        private class TypeWithNoBinderMetadata
        {
            public int UnMarkedProperty { get; set; }
        }

        private class HasAllGreedyProperties
        {
            [NonValueBinderMetadata]
            public string MarkedWithABinderMetadata { get; set; }
        }

        // Not a Metadata poco because there is a property with value binder Metadata.
        private class TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata
        {
            [NonValueBinderMetadata]
            public string MarkedWithABinderMetadata { get; set; }

            [ValueBinderMetadata]
            public string MarkedWithAValueBinderMetadata { get; set; }
        }

        // not a Metadata poco because there is an unmarked property.
        private class TypeWithUnmarkedAndBinderMetadataMarkedProperties
        {
            public int UnmarkedProperty { get; set; }

            [NonValueBinderMetadata]
            public string MarkedWithABinderMetadata { get; set; }
        }

        [Bind(new[] { nameof(IncludedExplicitly1), nameof(IncludedExplicitly2) })]
        private class TypeWithIncludedPropertiesUsingBindAttribute
        {
            public int ExcludedByDefault1 { get; set; }

            public int ExcludedByDefault2 { get; set; }

            public int IncludedExplicitly1 { get; set; }

            public int IncludedExplicitly2 { get; set; }
        }

        private class Document
        {
            [NonValueBinderMetadata]
            public string Version { get; set; }

            [NonValueBinderMetadata]
            public Document SubDocument { get; set; }
        }

        private class NonValueBinderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource
            {
                get { return new BindingSource("Special", string.Empty, isGreedy: true, isFromRequest: true); }
            }
        }

        private class ValueBinderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Query; } }
        }

        private class ExcludedProvider : IPropertyFilterProvider
        {
            public Func<ModelMetadata, bool> PropertyFilter
            {
                get
                {
                    return (m) =>
                       !string.Equals("Excluded1", m.PropertyName, StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals("Excluded2", m.PropertyName, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private class SimpleContainer
        {
            public Simple Simple { get; set; }
        }

        private class Simple
        {
            public string Name { get; set; }
        }

        private class CollectionContainer
        {
            public int[] ReadOnlyArray { get; } = new int[4];

            // Read-only collections get added values.
            public IDictionary<int, string> ReadOnlyDictionary { get; } = new Dictionary<int, string>();

            public IList<int> ReadOnlyList { get; } = new List<int>();

            // Settable values are overwritten.
            public int[] SettableArray { get; set; } = new int[] { 0, 1 };

            public IDictionary<int, string> SettableDictionary { get; set; } = new Dictionary<int, string>
            {
                { 0, "zero" },
                { 25, "twenty-five" },
            };

            public IList<int> SettableList { get; set; } = new List<int> { 3, 9, 0 };
        }

        private class TestableComplexTypeModelBinderProvider : IModelBinderProvider
        {
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                if (context.Metadata.IsComplexType)
                {
                    var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                    foreach (var property in context.Metadata.Properties)
                    {
                        propertyBinders.Add(property, context.CreateBinder(property));
                    }

                    return new TestableComplexTypeModelBinder(propertyBinders);
                }

                return null;
            }
        }

        // Provides the ability to easily mock + call each of these APIs
        public class TestableComplexTypeModelBinder : ComplexTypeModelBinder
        {
            public TestableComplexTypeModelBinder()
                : this(new Dictionary<ModelMetadata, IModelBinder>())
            {
            }

            public TestableComplexTypeModelBinder(bool allowValidatingTopLevelNodes)
                : this(new Dictionary<ModelMetadata, IModelBinder>(), allowValidatingTopLevelNodes)
            {
            }

            public TestableComplexTypeModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders)
                : base(propertyBinders, NullLoggerFactory.Instance)
            {
            }

            public TestableComplexTypeModelBinder(
                IDictionary<ModelMetadata, IModelBinder> propertyBinders,
                bool allowValidatingTopLevelNodes)
                : base(propertyBinders, NullLoggerFactory.Instance, allowValidatingTopLevelNodes)
            {
            }

            public Dictionary<ModelMetadata, ModelBindingResult> Results { get; } = new Dictionary<ModelMetadata, ModelBindingResult>();

            public virtual Task BindPropertyPublic(ModelBindingContext bindingContext)
            {
                if (Results.Count == 0)
                {
                    return base.BindModelAsync(bindingContext);
                }

                if (Results.TryGetValue(bindingContext.ModelMetadata, out var result))
                {
                    bindingContext.Result = result;
                }

                return Task.CompletedTask;
            }

            protected override Task BindProperty(ModelBindingContext bindingContext)
            {
                return BindPropertyPublic(bindingContext);
            }

            public virtual bool CanBindPropertyPublic(
                ModelBindingContext bindingContext,
                ModelMetadata propertyMetadata)
            {
                if (Results.Count == 0)
                {
                    return base.CanBindProperty(bindingContext, propertyMetadata);
                }

                // If this is being used to test binding, then only attempt to bind properties
                // we have results for.
                return Results.ContainsKey(propertyMetadata);
            }

            protected override bool CanBindProperty(
                ModelBindingContext bindingContext,
                ModelMetadata propertyMetadata)
            {
                return CanBindPropertyPublic(bindingContext, propertyMetadata);
            }

            public virtual object CreateModelPublic(ModelBindingContext bindingContext)
            {
                return base.CreateModel(bindingContext);
            }

            protected override object CreateModel(ModelBindingContext bindingContext)
            {
                return CreateModelPublic(bindingContext);
            }

            public virtual void SetPropertyPublic(
                ModelBindingContext bindingContext,
                string modelName,
                ModelMetadata propertyMetadata,
                ModelBindingResult result)
            {
                base.SetProperty(bindingContext, modelName, propertyMetadata, result);
            }

            protected override void SetProperty(
                ModelBindingContext bindingContext,
                string modelName,
                ModelMetadata propertyMetadata,
                ModelBindingResult result)
            {
                SetPropertyPublic(bindingContext, modelName, propertyMetadata, result);
            }
        }
    }
}
