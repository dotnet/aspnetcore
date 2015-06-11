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
        [InlineData(typeof(Person), true)]
        [InlineData(typeof(Person), false)]
        [InlineData(typeof(EmptyModel), true)]
        [InlineData(typeof(EmptyModel), false)]
        public async Task CanCreateModel_CreatesModel_ForTopLevelObjectIfThereIsExplicitPrefix(
            Type modelType,
            bool isPrefixProvided)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(false));

            var metadataProvider = new TestModelMetadataProvider();
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    // Random type.
                    ModelMetadata = metadataProvider.GetMetadataForType(typeof(Person)),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = metadataProvider,
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    },

                    // Setting it to empty ensures that model does not get created becasue of no model name.
                    ModelName = "dummyModelName",
                    BinderModelName = isPrefixProvided ? "prefix" : null,
                }
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(isPrefixProvided, retModel);
        }

        [Theory]
        [InlineData(typeof(Person), true)]
        [InlineData(typeof(Person), false)]
        [InlineData(typeof(EmptyModel), true)]
        [InlineData(typeof(EmptyModel), false)]
        public async Task
            CanCreateModel_CreatesModel_ForTopLevelObjectIfThereIsEmptyModelName(Type modelType, bool emptyModelName)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(false));

            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    // Random type.
                    ModelMetadata = GetMetadataForType(typeof(Person)),
                    ValueProvider = mockValueProvider.Object,
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                        ValueProvider = mockValueProvider.Object,
                        MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider()
                    }
                }
            };

            bindingContext.ModelBindingContext.ModelName = emptyModelName ? string.Empty : "dummyModelName";
            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(emptyModelName, retModel);
        }

        [Fact]
        public async Task CanCreateModel_ReturnsFalse_ForNonTopLevelModel_IfModelIsMarkedWithBinderMetadata()
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
            var canCreate = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.False(canCreate);
        }

        [Fact]
        public async Task CanCreateModel_ReturnsTrue_ForTopLevelModel_IfModelIsMarkedWithBinderMetadata()
        {
            var bindingContext = new MutableObjectBinderContext
            {
                ModelBindingContext = new ModelBindingContext
                {
                    // Here the metadata represents a top level object.
                    ModelMetadata = GetMetadataForType(typeof(Document)),
                    OperationBindingContext = new OperationBindingContext
                    {
                        ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                    }
                }
            };

            var mutableBinder = new MutableObjectModelBinder();

            // Act
            var canCreate = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.True(canCreate);
        }

        [Fact]
        public async Task CanCreateModel_CreatesModel_IfTheModelIsBinderPoco()
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(false));

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

                    // Setting it to empty ensures that model does not get created becasue of no model name.
                    ModelName = "dummyModelName",
                },
            };

            var mutableBinder = new TestableMutableObjectModelBinder();
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.True(retModel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanCreateModel_ReturnsTrue_ForNonTopLevelModel_BasedOnValueAvailability(bool valueAvailable)
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>(MockBehavior.Strict);
            mockValueProvider
                .Setup(provider => provider.ContainsPrefixAsync("SimpleContainer.Simple.Name"))
                .Returns(Task.FromResult(valueAvailable));

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
            var result = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            // Result matches whether first Simple property can bind.
            Assert.Equal(valueAvailable, result);
        }

        [Theory]
        [InlineData(typeof(TypeWithNoBinderMetadata), false)]
        [InlineData(typeof(TypeWithNoBinderMetadata), true)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), false)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), true)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), false)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), true)]
        public async Task
            CanCreateModel_CreatesModelForValueProviderBasedBinderMetadatas_IfAValueProviderProvidesValue
                (Type modelType, bool valueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(valueProviderProvidesValue));

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
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(valueProviderProvidesValue, retModel);
        }

        [Theory]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), false)]
        [InlineData(typeof(TypeWithAtLeastOnePropertyMarkedUsingValueBinderMetadata), true)]
        public async Task CanCreateModel_ForExplicitValueProviderMetadata_UsesOriginalValueProvider(
            Type modelType,
            bool originalValueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(false));

            var mockOriginalValueProvider = new Mock<IBindingSourceValueProvider>();
            mockOriginalValueProvider
                .Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(originalValueProviderProvidesValue));

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
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(originalValueProviderProvidesValue, retModel);
        }

        [Theory]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), false)]
        [InlineData(typeof(TypeWithUnmarkedAndBinderMetadataMarkedProperties), true)]
        [InlineData(typeof(TypeWithNoBinderMetadata), false)]
        [InlineData(typeof(TypeWithNoBinderMetadata), true)]
        public async Task CanCreateModel_UnmarkedProperties_UsesCurrentValueProvider(Type modelType, bool valueProviderProvidesValue)
        {
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(valueProviderProvidesValue));

            var mockOriginalValueProvider = new Mock<IValueProvider>();
            mockOriginalValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                                     .Returns(Task.FromResult(false));

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
            bindingContext.PropertyMetadata = mutableBinder.GetMetadataForProperties(
                                                                bindingContext.ModelBindingContext);

            // Act
            var retModel = await mutableBinder.CanCreateModel(bindingContext);

            // Assert
            Assert.Equal(valueProviderProvidesValue, retModel);
        }

        [Fact]
        public async Task BindModel_InitsInstance()
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(true));

            var mockDtoBinder = new Mock<IModelBinder>();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person)),
                ModelName = "someName",
                ValueProvider = mockValueProvider.Object,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = mockDtoBinder.Object,
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            mockDtoBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    // just return the DTO unchanged
                    return Task.FromResult(new ModelBindingResult(mbc.Model, mbc.ModelName, true));
                });

            var model = new Person();

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder
                .Setup(o => o.EnsureModelPublic(bindingContext))
                .Callback<ModelBindingContext>(c => c.Model = model)
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
        public async Task BindModel_InitsInstance_ForEmptyModelName()
        {
            // Arrange
            var mockValueProvider = new Mock<IValueProvider>();
            mockValueProvider.Setup(o => o.ContainsPrefixAsync(It.IsAny<string>()))
                             .Returns(Task.FromResult(false));

            var mockDtoBinder = new Mock<IModelBinder>();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person)),
                ModelName = "",
                ValueProvider = mockValueProvider.Object,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = mockDtoBinder.Object,
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };

            mockDtoBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    // just return the DTO unchanged
                    return Task.FromResult(new ModelBindingResult(mbc.Model, mbc.ModelName, true));
                });

            var model = new Person();

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder
                .Setup(o => o.EnsureModelPublic(bindingContext))
                .Callback<ModelBindingContext>(c => c.Model = model)
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
            var retModel = testableBinder.CreateModelPublic(bindingContext);

            // Assert
            Assert.IsType<Person>(retModel);
        }

        [Fact]
        public void EnsureModel_ModelIsNotNull_DoesNothing()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                Model = new Person(),
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };

            // Act
            var originalModel = bindingContext.Model;
            testableBinder.Object.EnsureModelPublic(bindingContext);

            // Assert
            Assert.Same(originalModel, bindingContext.Model);
            testableBinder.Verify(o => o.CreateModelPublic(bindingContext), Times.Never());
        }

        [Fact]
        public void EnsureModel_ModelIsNull_CallsCreateModel()
        {
            // Arrange
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person))
            };

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder.Setup(o => o.CreateModelPublic(bindingContext))
                          .Returns(new Person()).Verifiable();

            // Act
            var originalModel = bindingContext.Model;
            testableBinder.Object.EnsureModelPublic(bindingContext);
            var newModel = bindingContext.Model;

            // Assert
            Assert.Null(originalModel);
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
                // Any type, even an otherwise-simple POCO with an indexer property, would do here.
                ModelMetadata = GetMetadataForType(typeof(List<Person>)),
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
        public void ProcessDto_BindRequiredFieldMissing_RaisesModelError()
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
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);

            var nameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "Name");
            dto.Results[nameProperty] = new ModelBindingResult(
                "John Doe",
                isModelSet: true,
                key: "");
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_DataMemberIsRequiredFieldMissing_RaisesModelError()
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
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                    ValidatorProvider = Mock.Of<IModelValidatorProvider>()
                }
            };
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);

            var nameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "Name");
            dto.Results[nameProperty] = new ModelBindingResult(
                "John Doe",
                isModelSet: true,
                key: "");

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_ValueTypePropertyWithBindRequired_SetToNull_CapturesException()
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

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            var propertyMetadata = dto.PropertyMetadata.Single(o => o.PropertyName == "Name");
            dto.Results[propertyMetadata] = new ModelBindingResult(
                "John Doe",
                isModelSet: true,
                key: "theModel.Name");

            // Attempt to set non-Nullable property to null. BindRequiredAttribute should not be relevant in this
            // case because the binding exists.
            propertyMetadata = dto.PropertyMetadata.Single(o => o.PropertyName == "Age");
            dto.Results[propertyMetadata] = new ModelBindingResult(
                null,
                isModelSet: true,
                key: "theModel.Age");

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        [ReplaceCulture]
        public void ProcessDto_MissingDataForRequiredFields_NoErrors()
        {
            // Arrange
            var model = new ModelWithRequired();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);

            // Set no properties though Age (a non-Nullable struct) and City (a class) properties are required.
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
            Assert.Empty(modelStateDictionary);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessDto_ValueTypeProperty_WithRequiredAttribute_SetToNull_NoError()
        {
            // Arrange
            var model = new ModelWithRequired();
            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = CreateContext(containerMetadata, model);

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make Age valid and City invalid.
            var propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == "Age");
            dto.Results[propertyMetadata] = new ModelBindingResult(
                23,
                isModelSet: true,
                key: "theModel.Age");

            propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == "City");
            dto.Results[propertyMetadata] = new ModelBindingResult(
                null,
                isModelSet: true,
                key: "theModel.City");
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
            Assert.Empty(modelStateDictionary);
        }

        [Fact]
        public void ProcessDto_PropertyWithRequiredAttribute_NoPropertiesSet_NoError()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());
            var bindingContext = CreateContext(containerMetadata, model);

            // Set no properties though ValueTypeRequired (a non-Nullable struct) property is required.
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.True(modelStateDictionary.IsValid);
        }

        [Fact]
        public void ProcessDto_ValueTypeProperty_TriesToSetNullModel_CapturesException()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // The [DefaultValue] on ValueTypeRequiredWithDefaultValue is ignored by model binding.
            var expectedValue = 0;

            // Make ValueTypeRequired invalid.
            var propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == nameof(Person.ValueTypeRequired));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                null,
                isModelSet: true,
                key: "theModel." + nameof(Person.ValueTypeRequired));

            // Make ValueTypeRequiredWithDefaultValue invalid
            propertyMetadata = dto.PropertyMetadata
                .Single(p => p.PropertyName == nameof(Person.ValueTypeRequiredWithDefaultValue));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: null,
                isModelSet: true,
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue));

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_ValueTypeProperty_NoValue_NoError()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeRequired invalid.
            var propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == nameof(Person.ValueTypeRequired));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                null,
                isModelSet: false,
                key: "theModel." + nameof(Person.ValueTypeRequired));

            // Make ValueTypeRequiredWithDefaultValue invalid
            propertyMetadata = dto.PropertyMetadata
                .Single(p => p.PropertyName == nameof(Person.ValueTypeRequiredWithDefaultValue));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: null,
                isModelSet: false,
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue));

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

            // Assert
            Assert.True(modelStateDictionary.IsValid);
            Assert.Empty(modelStateDictionary);
        }

        [Fact]
        public void ProcessDto_ProvideRequiredFields_Success()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeRequired valid.
            var propertyMetadata = dto.PropertyMetadata
                .Single(p => p.PropertyName == nameof(Person.ValueTypeRequired));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                41,
                isModelSet: true,
                key: "theModel." + nameof(Person.ValueTypeRequired));

            // Make ValueTypeRequiredWithDefaultValue valid.
            propertyMetadata = dto.PropertyMetadata
                .Single(p => p.PropertyName == nameof(Person.ValueTypeRequiredWithDefaultValue));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: 57,
                isModelSet: true,
                key: "theModel." + nameof(Person.ValueTypeRequiredWithDefaultValue));

            // Also remind ProcessDto about PropertyWithDefaultValue -- as ComplexModelDtoModelBinder would.
            propertyMetadata = dto.PropertyMetadata
                .Single(p => p.PropertyName == nameof(Person.PropertyWithDefaultValue));
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: null,
                isModelSet: false,
                key: "theModel." + nameof(Person.PropertyWithDefaultValue));
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_ValueTypePropertyWithBindRequired_RequiredValidatorIgnored()
        {
            // Arrange
            var model = new ModelWithBindRequiredAndRequiredAttribute();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeProperty not have a value.
            var propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty)];
            dto.Results[propertyMetadata] = new ModelBindingResult(
                null,
                isModelSet: false,
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty));

            // Make ReferenceTypeProperty have a value.
            propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty)];
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: "value",
                isModelSet: true,
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty));

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_ReferenceTypePropertyWithBindRequired_RequiredValidatorIgnored()
        {
            // Arrange
            var model = new ModelWithBindRequiredAndRequiredAttribute();
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var modelStateDictionary = bindingContext.ModelState;

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeProperty have a value.
            var propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty)];
            dto.Results[propertyMetadata] = new ModelBindingResult(
                17,
                isModelSet: true,
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ValueTypeProperty));

            // Make ReferenceTypeProperty not have a value.
            propertyMetadata = containerMetadata
                .Properties[nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty)];
            dto.Results[propertyMetadata] = new ModelBindingResult(
                model: null,
                isModelSet: false,
                key: "theModel." + nameof(ModelWithBindRequiredAndRequiredAttribute.ReferenceTypeProperty));

            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

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
        public void ProcessDto_Success()
        {
            // Arrange
            var dob = new DateTime(2001, 1, 1);
            var model = new PersonWithBindExclusion
            {
                DateOfBirth = dob
            };
            var containerMetadata = GetMetadataForType(model.GetType());

            var bindingContext = CreateContext(containerMetadata, model);
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);

            var firstNameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "FirstName");
            dto.Results[firstNameProperty] = new ModelBindingResult(
                "John",
                isModelSet: true,
                key: "");

            var lastNameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "LastName");
            dto.Results[lastNameProperty] = new ModelBindingResult(
                "Doe",
                isModelSet: true,
                key: "");

            var dobProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "DateOfBirth");
            dto.Results[dobProperty] = null;
            var modelValidationNode = new ModelValidationNode(string.Empty, containerMetadata, model);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto, modelValidationNode);

            // Assert
            Assert.Equal("John", model.FirstName);
            Assert.Equal("Doe", model.LastName);
            Assert.Equal(dob, model.DateOfBirth);
            Assert.True(bindingContext.ModelState.IsValid);

            // Ensure that we add child nodes for all the nodes which have a result (irrespective of if they
            // are bound or not).
            Assert.Equal(2, modelValidationNode.ChildNodes.Count());

            var validationNode = modelValidationNode.ChildNodes[0];
            Assert.Equal("", validationNode.Key);
            Assert.Equal("John", validationNode.Model);

            validationNode = modelValidationNode.ChildNodes[1];
            Assert.Equal("", validationNode.Key);
            Assert.Equal("Doe", validationNode.Model);
        }

        [Fact]
        public void SetProperty_PropertyHasDefaultValue_DefaultValueAttributeDoesNothing()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForType(model.GetType()), model);

            var metadataProvider = bindingContext.OperationBindingContext.MetadataProvider;
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(Person), model);
            var propertyMetadata = bindingContext.ModelMetadata.Properties["PropertyWithDefaultValue"];

            var dtoResult = new ModelBindingResult(
                model: null,
                isModelSet: false,
                key: "foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["PropertyWithInitializedValue"];

            // This value won't be used because IsModelBound = false.
            var dtoResult = new ModelBindingResult(
                model: "bad-value",
                isModelSet: false,
                key: "foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["PropertyWithInitializedValueAndDefault"];

            // This value won't be used because IsModelBound = false.
            var dtoResult = new ModelBindingResult(
                model: "bad-value",
                isModelSet: false,
                key: "foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["NonUpdateableProperty"];

            var dtoResult = new ModelBindingResult(
                model: null,
                isModelSet: false,
                key: "foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var dtoResult = new ModelBindingResult(
                model: new Simple { Name = "Hanna" },
                isModelSet: true,
                key: propertyName);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var dtoResult = new ModelBindingResult(model: collection, isModelSet: true, key: propertyName);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["DateOfBirth"];

            var dtoResult = new ModelBindingResult(
                new DateTime(2001, 1, 1),
                key: "foo",
                isModelSet: true);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["DateOfDeath"];

            var dtoResult = new ModelBindingResult(
                new DateTime(1800, 1, 1),
                isModelSet: true,
                key: "foo");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["DateOfBirth"];

            var dtoResult = new ModelBindingResult(
                model: null,
                isModelSet: true,
                key: "foo.DateOfBirth");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
            var propertyMetadata = bindingContext.ModelMetadata.Properties["NameNoAttribute"];

            var dtoResult = new ModelBindingResult(
                model: null,
                isModelSet: true,
                key: "foo.NameNoAttribute");

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetProperty(
                bindingContext,
                modelExplorer,
                propertyMetadata,
                dtoResult);

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
                ModelState = new ModelStateDictionary(),
                ModelMetadata = metadata,
                ModelName = "theModel",
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

        private class Person
        {
            private DateTime? _dateOfDeath;

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

            [DefaultValue(typeof(decimal), "123.456")]
            public decimal PropertyWithDefaultValue { get; set; }

            public string PropertyWithInitializedValue { get; set; } = "preinitialized";

            [DefaultValue("default")]
            public string PropertyWithInitializedValueAndDefault { get; set; } = "preinitialized";
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

        private class ModelWithRequired
        {
            public string Name { get; set; }

            [Required]
            public int Age { get; set; }

            [Required]
            public string City { get; set; }
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

            public virtual void EnsureModelPublic(ModelBindingContext bindingContext)
            {
                base.EnsureModel(bindingContext);
            }

            protected override void EnsureModel(ModelBindingContext bindingContext)
            {
                EnsureModelPublic(bindingContext);
            }

            public virtual new IEnumerable<ModelMetadata> GetMetadataForProperties(ModelBindingContext bindingContext)
            {
                return base.GetMetadataForProperties(bindingContext);
            }

            public new void SetProperty(
                ModelBindingContext bindingContext,
                ModelExplorer modelExplorer,
                ModelMetadata propertyMetadata,
                ModelBindingResult dtoResult)
            {
                base.SetProperty(
                    bindingContext,
                    modelExplorer,
                    propertyMetadata,
                    dtoResult);
            }
        }
    }
}
#endif
