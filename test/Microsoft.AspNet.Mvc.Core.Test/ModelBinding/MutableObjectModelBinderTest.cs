// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MutableObjectModelBinderTest
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObject(
            bool isTopLevelObject,
            bool expectedCanCreate)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            var metadataProvider = new TestModelMetadataProvider();
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    IsTopLevelObject = isTopLevelObject,

                    // Random type.
                    ModelMetadata = metadataProvider.GetMetadataForType(typeof(Person)),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = metadataProvider,
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    },
                },
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(expectedCanCreate, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsFalse_IfNotIsTopLevelObjectAndModelIsMarkedWithBinderMetadata()
        {
            // Get the property metadata so that it is not a top level object.
            var modelMetadata = GetMetadataForType(typeof(Document))
                .Properties
                .First(metadata => metadata.PropertyName == nameof(Document.SubDocument));
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = modelMetadata,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    },
                    BindingSource = modelMetadata.BindingSource,
                    BinderModelName = modelMetadata.BinderModelName,
                }
            };

            var mutableBinder = new MutableObjectModelBinder();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.False(canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObjectAndModelIsMarkedWithBinderMetadata()
        {
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    // Here the metadata represents a top level object.
                    IsTopLevelObject = true,

                    ModelMetadata = GetMetadataForType(typeof(Document)),
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    }
                }
            };

            var mutableBinder = new MutableObjectModelBinder();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.True(canCreate);
        }

        [Fact]
        public void CanCreateModel_CreatesModel_IfTheModelIsBinderPoco()
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = GetMetadataForType(typeof(BinderMetadataPocoType)),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    },

                    // Setting it to empty ensures that model does not get created because of no model name.
                    ModelName = "dummyModelName",
                },
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.True(canCreate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanCreateModel_ReturnsTrue_IfNotIsTopLevelObject_BasedOnValueAvailability(
            bool valueAvailable)
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>(MockBehavior.Strict);
            mockValueProvider
                .Setup(provider => provider.ContainsPrefix("SimpleContainer.Simple.Name"))
                .Returns(valueAvailable);

            var typeMetadata = GetMetadataForType(typeof(SimpleContainer));
            var modelMetadata = typeMetadata.Properties[nameof(SimpleContainer.Simple)];
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = modelMetadata,
                    ModelName = "SimpleContainer.Simple",
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    },
                    ValueProvider = mockValueProvider.Object,
                },
                PropertyMetadata = modelMetadata.Properties,
            };

            var mutableBinder = new MutableObjectModelBinder();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            // Result matches whether first Simple property can bind.
            Assert.Equal(valueAvailable, canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsFalse_IfNotIsTopLevelObjectAndModelHasNoProperties()
        {
            // Arrange
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    IsTopLevelObject = false,

                    ModelMetadata = GetMetadataForType(typeof(PersonWithNoProperties))
                }
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.False(canCreate);
        }

        [Fact]
        public void CanCreateModel_ReturnsTrue_IfIsTopLevelObjectAndModelHasNoProperties()
        {
            // Arrange
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    IsTopLevelObject = true,
                    ModelMetadata = GetMetadataForType(typeof(PersonWithNoProperties))
                },
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.True(canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithNoBinderMetadata), false)]
        [InlineData(typeof(TypeWithNoBinderMetadata), true)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), false)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), true)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), false)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), true)]
        public void CanCreateModel_CreatesModelForValueProviderBasedBinderMetadatas_IfAValueProviderProvidesValue(
            Type modelType,
            bool valueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                             .Returns(valueProviderProvidesValue);

            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = GetMetadataForType(modelType),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    },
                    // Setting it to empty ensures that model does not get created becasue of no model name.
                    ModelName = "dummyName"
                }
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(valueProviderProvidesValue, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), false)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), true)]
        public void CanCreateModel_ForExplicitValueProviderMetadata_UsesOriginalValueProvider(
            Type modelType,
            bool originalValueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            var mockOriginalValueProvider = new Mock<IBindingSourceValueProvider>();
            mockOriginalValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(originalValueProviderProvidesValue);

            mockOriginalValueProvider
                .Setup(o => o.Filter(It.IsAny<BindingSource>()))
                .Returns<BindingSource>(source =>
                {
                    if (source == BindingSource.Query)
                    {
                        return mockOriginalValueProvider.Object;
                    }

                    return null;
                });

            var modelMetadata = GetMetadataForType(modelType);
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = modelMetadata,
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValueProvider = mockOriginalValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    },

                    // Setting it to empty ensures that model does not get created becasue of no model name.
                    ModelName = "dummyName",
                    BindingSource = modelMetadata.BindingSource,
                    BinderModelName = modelMetadata.BinderModelName
                }
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(originalValueProviderProvidesValue, canCreate);
        }

        [Theory]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), false)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), true)]
        [InlineData(typeof(TypeWithNoBinderMetadata), false)]
        [InlineData(typeof(TypeWithNoBinderMetadata), true)]
        public void CanCreateModel_UnmarkedProperties_UsesCurrentValueProvider(
            Type modelType,
            bool valueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                             .Returns(valueProviderProvidesValue);

            var mockOriginalValueProvider = new Mock<IValueProvider>();
            mockOriginalValueProvider.Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                                     .Returns(false);

            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    ModelMetadata = GetMetadataForType(modelType),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                        ValueProvider = mockOriginalValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    },
                    // Setting it to empty ensures that model does not get created becasue of no model name.
                    ModelName = "dummyName"
                }
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata =
                mutableBinder.GetMetadataForProperties(bindingContext.ModelBindingContext).ToArray();

            // Act
            var canCreate = mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(valueProviderProvidesValue, canCreate);
        }

        [Fact]
        public async Task BindModel_InitsInstance()
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(true);

            // Mock binder fails to bind all properties.
            var mockBinder = new Mock<IModelBinder>();
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.NoResultAsync);

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person)),
                ModelName = "someName",
                ValueProvider = mockValueProvider.Object,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = mockBinder.Object,
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            var model = new Person();

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder
                .Setup(o => o.GetModelPublic(bindingContext))
                .Returns(model)
                .Verifiable();
            testableBinder
                .Setup(o => o.GetMetadataForProperties(bindingContext))
                .Returns(new ModelMetadata[0]);

            // Act
            var retValue = await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retValue);
            Assert.True(retValue.IsModelSet);
            var returnedPerson = Assert.IsType<Person>(retValue.Model);
            Assert.Same(model, returnedPerson);
            testableBinder.Verify();
        }

        [Fact]
        public async Task BindModel_InitsInstance_IfIsTopLevelObject()
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider
                .Setup(o => o.ContainsPrefix(It.IsAny<string>()))
                .Returns(false);

            // Mock binder fails to bind all properties.
            var mockBinder = new Mock<IModelBinder>();
            mockBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.NoResultAsync);

            var bindingContext = new ModelBindingContext
            {
                IsTopLevelObject = true,
                ModelMetadata = GetMetadataForType(typeof(Person)),
                ModelName = string.Empty,
                ValueProvider = mockValueProvider.Object,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = mockBinder.Object,
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            var model = new Person();

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder
                .Setup(o => o.GetModelPublic(bindingContext))
                .Returns(model)
                .Verifiable();

            testableBinder
                .Setup(o => o.GetMetadataForProperties(bindingContext))
                .Returns(new ModelMetadata[0]);

            // Act
            var retValue = await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retValue);
            Assert.True(retValue.IsModelSet);
            var returnedPerson = Assert.IsType<Person>(retValue.Model);
            Assert.Same(model, returnedPerson);
            testableBinder.Verify();
        }

        [Theory]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyArray), false)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyInt), false)]    // read-only value type
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyObject), true)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlySimple), true)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadOnlyString), false)]
        [InlineData(nameof(MyModelTestingCanUpdateProperty.ReadWriteString), true)]
        public void CanUpdateProperty_ReturnsExpectedValue(string propertyName, bool expected)
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty(propertyName);

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

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
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = metadataProvider.GetMetadataForProperty(typeof(CollectionContainer), propertyName);

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(metadata);

            // Assert
            Assert.Equal(expected, canUpdate);
        }

        [Fact]
        public void CreateModel_InstantiatesInstanceOfMetadataType()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var model = testableBinder.CreateModelPublic(bindingContext);

            // Assert
            Assert.IsType<Person>(model);
        }

        [Fact]
        public void GetModel_ModelIsNotNull_DoesNothing()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                Model = new Person(),
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var originalModel = bindingContext.Model;
            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };

            // Act
            var newModel = testableBinder.Object.GetModelPublic(bindingContext);

            // Assert
            Assert.Same(originalModel, bindingContext.Model);
            Assert.Same(originalModel, newModel);
            testableBinder.Verify(o => o.CreateModelPublic(bindingContext), Times.Never());
        }

        [Fact]
        public void GetModel_ModelIsNull_CallsCreateModel()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var originalModel = bindingContext.Model;
            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder.Setup(o => o.CreateModelPublic(bindingContext))
                          .Returns(new Person()).Verifiable();

            // Act
            var newModel = testableBinder.Object.GetModelPublic(bindingContext);


            // Assert
            Assert.Null(originalModel);
            Assert.Null(bindingContext.Model);
            Assert.IsType<Person>(newModel);
            testableBinder.Verify();
        }

        [Fact]
        public void GetMetadataForProperties_WithBindAttribute()
        {
            // Arrange
            var expectedPropertyNames = new[] { "FirstName", "LastName" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(PersonWithBindExclusion)),
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider()
                }
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForProperties(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Fact]
        public void GetMetadataForProperties_WithoutBindAttribute()
        {
            // Arrange
            var expectedPropertyNames = new[]
            {
                nameof(Person.DateOfBirth),
                nameof(Person.DateOfDeath),
                nameof(Person.ValueTypeRequired),
                nameof(Person.ValueTypeRequiredWithDefaultValue),
                nameof(Person.FirstName),
                nameof(Person.LastName),
                nameof(Person.PropertyWithDefaultValue),
                nameof(Person.PropertyWithInitializedValue),
                nameof(Person.PropertyWithInitializedValueAndDefault),
            };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person)),
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider()
                },
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForProperties(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Fact]
        public void GetMetadataForProperties_DoesNotReturn_ExcludedProperties()
        {
            // Arrange
            var expectedPropertyNames = new[] { "IncludedByDefault1", "IncludedByDefault2" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(TypeWithExcludedPropertiesUsingBindAttribute)),
                OperationBindingContext = new OperationBindingContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = CreateServices()
                    },
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                }
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForProperties(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Fact]
        public void GetMetadataForProperties_ReturnsOnlyIncludedProperties_UsingBindAttributeInclude()
        {
            // Arrange
            var expectedPropertyNames = new[] { "IncludedExplicitly1", "IncludedExplicitly2" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(TypeWithIncludedPropertiesUsingBindAttribute)),
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                }
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForProperties(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Fact]
        public void GetRequiredPropertiesCollection_MixedAttributes()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(ModelWithMixedBindingBehaviors)),
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            // Act
            var validationInfo = MutableObjectModelBinder.GetPropertyValidationInfo(bindingContext);

            // Assert
            Assert.Equal(new[] { "Required" }, validationInfo.RequiredProperties);
            Assert.Equal(new[] { "Never" }, validationInfo.SkipProperties);
        }

        [Fact]
        public void GetPropertyValidationInfo_WithIndexerProperties_Succeeds()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(PersonCollection)),
                OperationBindingContext = new OperationBindingContext
                {
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                },
            };

            // Act
            var validationInfo = MutableObjectModelBinder.GetPropertyValidationInfo(bindingContext);

            // Assert
            Assert.Equal(Enumerable.Empty<string>(), validationInfo.RequiredProperties);
            Assert.Equal(Enumerable.Empty<string>(), validationInfo.SkipProperties);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessResults_BindRequiredFieldMissing_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithBindRequired
            {
                Name = "original value",
                Age = -20
            };

            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = new ModelBindingContext
            {
                Model = model,
                ModelMetadata = containerMetadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var nameProperty = containerMetadata.Properties[nameof(model.Name)];
            results[nameProperty] = ModelBindingResult.Success(string.Empty, "John Doe");
            
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Single(modelStateDictionary);

            // Check Age error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            var modelError = Assert.Single(modelState.Errors);
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("A value for the 'Age' property was not provided.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessResults_DataMemberIsRequiredFieldMissing_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithDataMemberIsRequired
            {
                Name = "original value",
                Age = -20
            };

            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = new ModelBindingContext
            {
                Model = model,
                ModelMetadata = containerMetadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var nameProperty = containerMetadata.Properties[nameof(model.Name)];
            results[nameProperty] = ModelBindingResult.Success(string.Empty, "John Doe");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Single(modelStateDictionary);

            // Check Age error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            var modelError = Assert.Single(modelState.Errors);
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("A value for the 'Age' property was not provided.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessResults_ValueTypePropertyWithBindRequired_SetToNull_CapturesException()
        {
            // Arrange
            var model = new ModelWithBindRequired
            {
                Name = "original value",
                Age = -20
            };

            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = new ModelBindingContext()
            {
                Model = model,
                ModelMetadata = containerMetadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var propertyMetadata = containerMetadata.Properties[nameof(model.Name)];
            results[propertyMetadata] = ModelBindingResult.Success("theModel.Name", "John Doe");

            // Attempt to set non-Nullable property to null. BindRequiredAttribute should not be relevant in this
            // case because the binding exists.
            propertyMetadata = containerMetadata.Properties[nameof(model.Age)];
            results[propertyMetadata] = ModelBindingResult.Success("theModel.Age", model: null);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.False(modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.Count);

            // Check Age error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

            var modelError = Assert.Single(modelState.Errors);
            Assert.Equal(string.Empty, modelError.ErrorMessage);
            Assert.IsType<NullReferenceException>(modelError.Exception);
        }

        [Fact]
        public void ProcessResults_ValueTypeProperty_WithBindingOptional_NoValueSet_NoError()
        {
            // Arrange
            var model = new BindingOptionalProperty();
            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = CreateContext(containerMetadata, model);

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
        }

        [Fact]
        public void ProcessResults_NullableValueTypeProperty_NoValueSet_NoError()
        {
            // Arrange
            var model = new NullableValueTypeProperty();
            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = CreateContext(containerMetadata, model);

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
        }

        [Fact]
        public void ProcessResults_ValueTypeProperty_TriesToSetNullModel_CapturesException()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var testableBinder = new TestableMutableObjectModelBinder();

            // The [DefaultValue] on ValueTypeRequiredWithDefaultValue is ignored by model binding.
            var expectedValue = 0;

            // Make ValueTypeRequired invalid.
            var propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequired)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(Person.ValueTypeRequired),
                model: null);

            // Make ValueTypeRequiredWithDefaultValue invalid
            propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequiredWithDefaultValue)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue),
                model: null);

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            Assert.False(modelStateDictionary.IsValid);

            // Check ValueTypeRequired error.
            var modelStateEntry = Assert.Single(
                modelStateDictionary,
                entry => entry.Key == "theModel." + nameof(Person.ValueTypeRequired));
            Assert.Equal("theModel." + nameof(Person.ValueTypeRequired), modelStateEntry.Key);

            var modelState = modelStateEntry.Value;
            Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

            var error = Assert.Single(modelState.Errors);
            Assert.Equal(string.Empty, error.ErrorMessage);
            Assert.IsType<NullReferenceException>(error.Exception);

            // Check ValueTypeRequiredWithDefaultValue error.
            modelStateEntry = Assert.Single(
                modelStateDictionary,
                entry => entry.Key == "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue));
            Assert.Equal("theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue), modelStateEntry.Key);

            modelState = modelStateEntry.Value;
            Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

            error = Assert.Single(modelState.Errors);
            Assert.Equal(string.Empty, error.ErrorMessage);
            Assert.IsType<NullReferenceException>(error.Exception);

            Assert.Equal(0, model.ValueTypeRequired);
            Assert.Equal(expectedValue, model.ValueTypeRequiredWithDefaultValue);
        }

        [Fact]
        public void ProcessResults_ValueTypeProperty_NoValue_NoError()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelState = bindingContext.ModelState;

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeRequired invalid.
            var propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequired)];
            results[propertyMetadata] = ModelBindingResult.Failed("theModel." + nameof(Person.ValueTypeRequired));

            // Make ValueTypeRequiredWithDefaultValue invalid
            propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequiredWithDefaultValue)];
            results[propertyMetadata] = ModelBindingResult.Failed(
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue));

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public void ProcessResults_ProvideRequiredFields_Success()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeRequired valid.
            var propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequired)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(Person.ValueTypeRequired),
                model: 41);

            // Make ValueTypeRequiredWithDefaultValue valid.
            propertyMetadata = containerMetadata.Properties[nameof(Person.ValueTypeRequiredWithDefaultValue)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue),
                model: 57);

            // Also remind ProcessResults about PropertyWithDefaultValue -- as BindPropertiesAsync() would.
            propertyMetadata = containerMetadata.Properties[nameof(Person.PropertyWithDefaultValue)];
            results[propertyMetadata] = ModelBindingResult.Failed(
                key: "theModel." + nameof(Person.PropertyWithDefaultValue));

            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            Assert.True(modelStateDictionary.IsValid);
            Assert.Empty(modelStateDictionary);

            // Model gets provided values.
            Assert.Equal(41, model.ValueTypeRequired);
            Assert.Equal(57, model.ValueTypeRequiredWithDefaultValue);
            Assert.Equal(0m, model.PropertyWithDefaultValue);     // [DefaultValue] has no effect
        }

        // [Required] cannot provide a custom validation for [BindRequired] errors.
        [Fact]
        public void ProcessResults_ValueTypePropertyWithBindRequired_RequiredValidatorIgnored()
        {
            // Arrange
            var model = new ModelWithBindRequiredAndRequiredAttribute();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeProperty not have a value.
            var propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty)];
            results[propertyMetadata] = ModelBindingResult.Failed(
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty));

            // Make ReferenceTypeProperty have a value.
            propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty),
                model: "value");
            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            Assert.False(modelStateDictionary.IsValid);

            var entry = Assert.Single(
                modelStateDictionary,
                kvp => kvp.Key == "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty))
                .Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("A value for the 'ValueTypeProperty' property was not provided.", error.ErrorMessage);

            // Model gets provided values.
            Assert.Equal(0, model.ValueTypeProperty);
            Assert.Equal("value", model.ReferenceTypeProperty);
        }

        // [Required] cannot provide a custom validation for [BindRequired] errors.
        [Fact]
        public void ProcessResults_ReferenceTypePropertyWithBindRequired_RequiredValidatorIgnored()
        {
            // Arrange
            var model = new ModelWithBindRequiredAndRequiredAttribute();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeProperty have a value.
            var propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty)];
            results[propertyMetadata] = ModelBindingResult.Success(
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty),
                model: 17);

            // Make ReferenceTypeProperty not have a value.
            propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty)];
            results[propertyMetadata] = ModelBindingResult.Failed(
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty));
            // Act
            testableBinder.ProcessResults(bindingContext, results);

            // Assert
            Assert.False(modelStateDictionary.IsValid);

            var entry = Assert.Single(
                modelStateDictionary,
                kvp => kvp.Key == "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty))
                .Value;
            var error = Assert.Single(entry.Errors);
            Assert.Null(error.Exception);
            Assert.Equal("A value for the 'ReferenceTypeProperty' property was not provided.", error.ErrorMessage);

            // Model gets provided values.
            Assert.Equal(17, model.ValueTypeProperty);
            Assert.Null(model.ReferenceTypeProperty);
        }


        [Fact]
        public void ProcessResults_Success()
        {
            // Arrange
            var dob = new DateTime(2001, 1, 1);
            var model = new PersonWithBindExclusion
            {
                DateOfBirth = dob
            };
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var results = containerMetadata.Properties.ToDictionary(
                property => property,
                property => ModelBindingResult.Failed(property.PropertyName));

            var firstNameProperty = containerMetadata.Properties[nameof(model.FirstName)];
            results[firstNameProperty] = ModelBindingResult.Success(
                nameof(model.FirstName),
                "John");

            var lastNameProperty = containerMetadata.Properties[nameof(model.LastName)];
            results[lastNameProperty] = ModelBindingResult.Success(
                nameof(model.LastName),
                "Doe");
            
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessResults(bindingContext, results);

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

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.PropertyWithDefaultValue)];

            var result = ModelBindingResult.Failed("foo");
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

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

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.PropertyWithInitializedValue)];

            // The null model value won't be used because IsModelBound = false.
            var result = ModelBindingResult.Failed("foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

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

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata =
                bindingContext.ModelMetadata.Properties[nameof(model.PropertyWithInitializedValueAndDefault)];

            // The null model value won't be used because IsModelBound = false.
            var result = ModelBindingResult.Failed("foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

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

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.NonUpdateableProperty)];

            var result = ModelBindingResult.Failed("foo");
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

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

        // Reviewers: Is this inconsistency with CanUpdateProperty() an issue we should be tracking?
        [Theory]
        [MemberData(nameof(MyCanUpdateButCannotSetPropertyData))]
        public void SetProperty_ValueProvidedAndCanUpdatePropertyTrue_DoesNothing(
            string propertyName,
            Func<object, object> propertAccessor)
        {
            // Arrange
            var model = new MyModelTestingCanUpdateProperty();
            var type = model.GetType();
            var bindingContext = CreateContext(GetMetadataForType(type), model);
            var modelState = bindingContext.ModelState;
            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(type, model);

            var propertyMetadata = bindingContext.ModelMetadata.Properties[propertyName];
            var result = ModelBindingResult.Success(
                propertyName,
                new Simple { Name = "Hanna" });

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

            // Assert
            Assert.Equal("Joe", propertAccessor(model));
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        // Property name, property accessor, collection.
        public static TheoryData<string, Func<object, object>, object> CollectionPropertyData
        {
            get
            {
                return new TheoryData<string, Func<object, object>, object>
                {
                    {
                        nameof(CollectionContainer.ReadOnlyDictionary),
                        model => ((CollectionContainer)model).ReadOnlyDictionary,
                        new Dictionary<int, string>
                        {
                            { 1, "one" },
                            { 2, "two" },
                            { 3, "three" },
                        }
                    },
                    {
                        nameof(CollectionContainer.ReadOnlyList),
                        model => ((CollectionContainer)model).ReadOnlyList,
                        new List<int> { 1, 2, 3, 4 }
                    },
                    {
                        nameof(CollectionContainer.SettableArray),
                        model => ((CollectionContainer)model).SettableArray,
                        new int[] { 1, 2, 3, 4 }
                    },
                    {
                        nameof(CollectionContainer.SettableDictionary),
                        model => ((CollectionContainer)model).SettableDictionary,
                        new Dictionary<int, string>
                        {
                            { 1, "one" },
                            { 2, "two" },
                            { 3, "three" },
                        }
                    },
                    {
                        nameof(CollectionContainer.SettableList),
                        model => ((CollectionContainer)model).SettableList,
                        new List<int> { 1, 2, 3, 4 }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CollectionPropertyData))]
        public void SetProperty_CollectionProperty_UpdatesModel(
            string propertyName,
            Func<object, object> propertyAccessor,
            object collection)
        {
            // Arrange
            var model = new CollectionContainer();
            var type = model.GetType();
            var bindingContext = CreateContext(GetMetadataForType(type), model);
            var modelState = bindingContext.ModelState;
            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(type, model);

            var propertyMetadata = bindingContext.ModelMetadata.Properties[propertyName];
            var result = ModelBindingResult.Success(propertyName, collection);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

            // Assert
            Assert.Equal(collection, propertyAccessor(model));
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public void SetProperty_PropertyIsSettable_CallsSetter()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.DateOfBirth)];

            var result = ModelBindingResult.Success("foo", new DateTime(2001, 1, 1));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

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

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.DateOfDeath)];

            var result = ModelBindingResult.Success("foo", new DateTime(1800, 1, 1));
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

            // Assert
            Assert.Equal("Date of death can't be before date of birth." + Environment.NewLine
                       + "Parameter name: value",
                         bindingContext.ModelState["foo"].Errors[0].Exception.Message);
        }

        // This can only really be done by writing an invalid model binder and returning 'isModelSet: true'
        // with a null model for a value type.
        [Fact]
        public void SetProperty_SettingNonNullableValueTypeToNull_CapturesException()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.DateOfBirth)];

            var result = ModelBindingResult.Success("foo.DateOfBirth", model: null);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

            // Assert
            Assert.False(bindingContext.ModelState.IsValid);

            var entry = Assert.Single(bindingContext.ModelState, kvp => kvp.Key == "foo.DateOfBirth").Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal(string.Empty, error.ErrorMessage);
            Assert.IsType<NullReferenceException>(error.Exception);
        }

        [Fact]
        [ReplaceCulture]
        public void SetProperty_PropertySetterThrows_CapturesException()
        {
            // Arrange
            var model = new ModelWhosePropertySetterThrows();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);
            bindingContext.ModelName = "foo";

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(ModelWhosePropertySetterThrows), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties[nameof(model.NameNoAttribute)];

            var result = ModelBindingResult.Success("foo.NameNoAttribute", model: null);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);

            // Assert
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Equal(1, bindingContext.ModelState["foo.NameNoAttribute"].Errors.Count);
            Assert.Equal("This is a different exception." + Environment.NewLine
                       + "Parameter name: value",
                         bindingContext.ModelState["foo.NameNoAttribute"].Errors[0].Exception.Message);
        }

        private static ModelBindingContext CreateContext(ModelMetadata metadata, object model)
        {
            return new ModelBindingContext
            {
                Model = model,
                ModelMetadata = metadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = TestModelValidatorProvider.CreateDefaultProvider(),
                }
            };
        }

        private static ModelMetadata GetMetadataForCanUpdateProperty(string propertyName)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            return metadataProvider.GetMetadataForProperty(typeof(MyModelTestingCanUpdateProperty), propertyName);
        }

        private static ModelMetadata GetMetadataForType(Type t)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            return metadataProvider.GetMetadataForType(t);
        }

        private class EmptyModel
        {
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

            [Required(ErrorMessage = "Another sample message")]
            [DefaultValue(42)]
            public int ValueTypeRequiredWithDefaultValue { get; set; }

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

        [BindRequired]
        private class ModelWithBindRequiredAndRequiredAttribute
        {
            [Range(5, 20)]
            [Required(ErrorMessage = "Custom Message {0}")]
            public int ValueTypeProperty { get; set; }

            [StringLength(25)]
            [Required(ErrorMessage = "Custom Message {0}")]
            public string ReferenceTypeProperty { get; set; }
        }

        private sealed class MyModelTestingCanUpdateProperty
        {
            public int ReadOnlyInt { get; private set; }
            public string ReadOnlyString { get; private set; }
            public string[] ReadOnlyArray { get; private set; }
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

        private class BinderMetadataPocoType
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

        [Bind(typeof(ExcludedProvider))]
        private class TypeWithExcludedPropertiesUsingBindAttribute
        {
            public int Excluded1 { get; set; }

            public int Excluded2 { get; set; }

            public int IncludedByDefault1 { get; set; }
            public int IncludedByDefault2 { get; set; }
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
            public BindingSource BindingSource { get { return BindingSource.Body; } }
        }

        private class ValueBinderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Query; } }
        }

        private class ExcludedProvider : IPropertyBindingPredicateProvider
        {
            public Func<ModelBindingContext, string, bool> PropertyFilter
            {
                get
                {
                    return (context, propertyName) =>
                       !string.Equals("Excluded1", propertyName, StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals("Excluded2", propertyName, StringComparison.OrdinalIgnoreCase);
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

        private class PersonCollection
        {
            public Person this[int index]
            {
                get
                {
                    return null;
                }
            }
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

        private IServiceProvider CreateServices()
        {
            var services = new Mock<IServiceProvider>(MockBehavior.Strict);
            return services.Object;
        }

        public class TestableMutableObjectModelBinder : MutableObjectModelBinder
        {
            public virtual bool CanUpdatePropertyPublic(ModelMetadata propertyMetadata)
            {
                return base.CanUpdateProperty(propertyMetadata);
            }

            protected override bool CanUpdateProperty(ModelMetadata propertyMetadata)
            {
                return CanUpdatePropertyPublic(propertyMetadata);
            }

            public virtual object CreateModelPublic(ModelBindingContext bindingContext)
            {
                return base.CreateModel(bindingContext);
            }

            protected override object CreateModel(ModelBindingContext bindingContext)
            {
                return CreateModelPublic(bindingContext);
            }

            public virtual object GetModelPublic(ModelBindingContext bindingContext)
            {
                return base.GetModel(bindingContext);
            }

            protected override object GetModel(ModelBindingContext bindingContext)
            {
                return GetModelPublic(bindingContext);
            }

            public virtual new IEnumerable<ModelMetadata> GetMetadataForProperties(ModelBindingContext bindingContext)
            {
                return base.GetMetadataForProperties(bindingContext);
            }

            public new void SetProperty(
                ModelBindingContext bindingContext,
                ModelExplorer modelExplorer,
                ModelMetadata propertyMetadata,
                ModelBindingResult result)
            {
                base.SetProperty(bindingContext, modelExplorer, propertyMetadata, result);
            }
        }
    }
}
#endif
