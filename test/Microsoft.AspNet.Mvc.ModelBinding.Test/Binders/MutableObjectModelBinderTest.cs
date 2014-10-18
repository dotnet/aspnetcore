// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MutableObjectModelBinderTest
    {
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
                ModelMetadata = GetMetadataForObject(new Person()),
                ModelName = "someName",
                ValueProvider = mockValueProvider.Object,
                ModelBinder = mockDtoBinder.Object,
                MetadataProvider = new DataAnnotationsModelMetadataProvider(),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>()
            };

            mockDtoBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    // just return the DTO unchanged
                    return Task.FromResult(true);
                });

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder.Setup(o => o.EnsureModelPublic(bindingContext)).Verifiable();
            testableBinder.Setup(o => o.GetMetadataForPropertiesPublic(bindingContext))
                              .Returns(new ModelMetadata[0]).Verifiable();

            // Act
            var retValue = await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retValue);
            Assert.IsType<Person>(bindingContext.Model);
            Assert.True(bindingContext.ValidationNode.ValidateAllProperties);
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
                ModelMetadata = GetMetadataForObject(new Person()),
                ModelName = "",
                ValueProvider = mockValueProvider.Object,
                ModelBinder = mockDtoBinder.Object,
                MetadataProvider = new DataAnnotationsModelMetadataProvider(),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>()
            };

            mockDtoBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    // just return the DTO unchanged
                    return Task.FromResult(true);
                });

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };
            testableBinder.Setup(o => o.EnsureModelPublic(bindingContext)).Verifiable();
            testableBinder.Setup(o => o.GetMetadataForPropertiesPublic(bindingContext))
                              .Returns(new ModelMetadata[0]).Verifiable();

            // Act
            var retValue = await testableBinder.Object.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retValue);
            Assert.IsType<Person>(bindingContext.Model);
            Assert.True(bindingContext.ValidationNode.ValidateAllProperties);
            testableBinder.Verify();
        }

        [Fact]
        public void CanUpdateProperty_HasPublicSetter_ReturnsTrue()
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty("ReadWriteString");

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.True(canUpdate);
        }

        [Fact]
        public void CanUpdateProperty_ReadOnlyArray_ReturnsFalse()
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty("ReadOnlyArray");

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.False(canUpdate);
        }

        [Fact]
        public void CanUpdateProperty_ReadOnlyReferenceTypeNotBlacklisted_ReturnsTrue()
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty("ReadOnlyObject");

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.True(canUpdate);
        }

        [Fact]
        public void CanUpdateProperty_ReadOnlyString_ReturnsFalse()
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty("ReadOnlyString");

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.False(canUpdate);
        }

        [Fact]
        public void CanUpdateProperty_ReadOnlyValueType_ReturnsFalse()
        {
            // Arrange
            var propertyMetadata = GetMetadataForCanUpdateProperty("ReadOnlyInt");

            // Act
            var canUpdate = MutableObjectModelBinder.CanUpdatePropertyInternal(propertyMetadata);

            // Assert
            Assert.False(canUpdate);
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
                ModelMetadata = GetMetadataForObject(new Person())
            };

            var testableBinder = new Mock<TestableMutableObjectModelBinder> { CallBase = true };

            // Act
            var originalModel = bindingContext.Model;
            testableBinder.Object.EnsureModelPublic(bindingContext);
            var newModel = bindingContext.Model;

            // Assert
            Assert.Same(originalModel, newModel);
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
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                MetadataProvider = new DataAnnotationsModelMetadataProvider()
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForPropertiesPublic(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Fact]
        public void GetMetadataForProperties_WithoutBindAttribute()
        {
            // Arrange
            var expectedPropertyNames = new[] { "DateOfBirth", "DateOfDeath", "ValueTypeRequired", "FirstName", "LastName", "PropertyWithDefaultValue" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(Person)),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                MetadataProvider = new DataAnnotationsModelMetadataProvider()
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForPropertiesPublic(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Bind(Exclude = nameof(Excluded1) + "," + nameof(Excluded2))]
        private class TypeWithExcludedPropertiesUsingBindAttribute
        {
            public int Excluded1 { get; set; }

            public int Excluded2 { get; set; }

            public int IncludedByDefault1 { get; set; }
            public int IncludedByDefault2 { get; set; }
        }

        [Fact]
        public void GetMetadataForProperties_DoesNotReturn_ExcludedProperties()
        {
            // Arrange
            var expectedPropertyNames = new[] { "IncludedByDefault1", "IncludedByDefault2" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(TypeWithExcludedPropertiesUsingBindAttribute)),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                MetadataProvider = new DataAnnotationsModelMetadataProvider()
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForPropertiesPublic(bindingContext);
            var returnedPropertyNames = propertyMetadatas.Select(o => o.PropertyName).ToArray();

            // Assert
            Assert.Equal(expectedPropertyNames, returnedPropertyNames);
        }

        [Bind(Include = nameof(IncludedExplicitly1) + "," + nameof(IncludedExplicitly2))]
        private class TypeWithIncludedPropertiesUsingBindAttribute
        {
            public int ExcludedByDefault1 { get; set; }

            public int ExcludedByDefault2 { get; set; }

            public int IncludedExplicitly1 { get; set; }

            public int IncludedExplicitly2 { get; set; }
        }

        [Fact]
        public void GetMetadataForProperties_ReturnsOnlyWhiteListedProperties_UsingBindAttributeInclude()
        {
            // Arrange
            var expectedPropertyNames = new[] { "IncludedExplicitly1", "IncludedExplicitly2" };
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = GetMetadataForType(typeof(TypeWithIncludedPropertiesUsingBindAttribute)),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>(),
                MetadataProvider = new DataAnnotationsModelMetadataProvider()
            };

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            var propertyMetadatas = testableBinder.GetMetadataForPropertiesPublic(bindingContext);
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
                ModelMetadata = GetMetadataForObject(new ModelWithMixedBindingBehaviors()),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>()
            };

            // Act
            var validationInfo = MutableObjectModelBinder.GetPropertyValidationInfo(bindingContext);

            // Assert
            Assert.Equal(new[] { "Required" }, validationInfo.RequiredProperties);
            Assert.Equal(new[] { "Never" }, validationInfo.SkipProperties);
        }

        [Fact]
        public void NullCheckFailedHandler_ModelStateAlreadyInvalid_DoesNothing()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("foo.bar", "Some existing error.");

            var modelMetadata = GetMetadataForType(typeof(Person));
            var validationNode = new ModelValidationNode(modelMetadata, "foo");
            var validationContext = new ModelValidationContext(new DataAnnotationsModelMetadataProvider(),
                                                               Mock.Of<IModelValidatorProvider>(),
                                                               modelState,
                                                               modelMetadata,
                                                               null);
            var e = new ModelValidatedEventArgs(validationContext, parentNode: null);

            // Act
            var handler = MutableObjectModelBinder.CreateNullCheckFailedHandler(modelMetadata, incomingValue: null);
            handler(validationNode, e);

            // Assert
            Assert.False(modelState.ContainsKey("foo"));
        }

        [Fact]
        public void NullCheckFailedHandler_ModelStateValid_AddsErrorString()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            var modelMetadata = GetMetadataForType(typeof(Person));
            var validationNode = new ModelValidationNode(modelMetadata, "foo");
            var validationContext = new ModelValidationContext(new DataAnnotationsModelMetadataProvider(),
                                                               Mock.Of<IModelValidatorProvider>(),
                                                               modelState,
                                                               modelMetadata,
                                                               null);
            var e = new ModelValidatedEventArgs(validationContext, parentNode: null);

            // Act
            var handler = MutableObjectModelBinder.CreateNullCheckFailedHandler(modelMetadata, incomingValue: null);
            handler(validationNode, e);

            // Assert
            Assert.True(modelState.ContainsKey("foo"));
            Assert.Equal("A value is required.", modelState["foo"].Errors[0].ErrorMessage);
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

            var containerMetadata = GetMetadataForObject(model);
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = containerMetadata,
                ModelName = "theModel",
                ValidatorProvider = Mock.Of<IModelValidatorProvider>()
            };
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);

            var nameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "Name");
            dto.Results[nameProperty] = new ComplexModelDtoResult("John Doe", new ModelValidationNode(nameProperty, ""));

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.Count);

            // Check Age error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("The 'Age' property is required.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessDto_BindRequiredFieldNull_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithBindRequired
            {
                Name = "original value",
                Age = -20
            };

            var containerMetadata = GetMetadataForObject(model);
            var bindingContext = new ModelBindingContext()
            {
                ModelMetadata = containerMetadata,
                ModelName = "theModel",
                ModelState = new ModelStateDictionary(),
                ValidatorProvider = Mock.Of<IModelValidatorProvider>()
            };
            var validationContext = new ModelValidationContext(new EmptyModelMetadataProvider(),
                                                               bindingContext.ValidatorProvider,
                                                               bindingContext.ModelState,
                                                               containerMetadata,
                                                               null);

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            var propertyMetadata = dto.PropertyMetadata.Single(o => o.PropertyName == "Name");
            dto.Results[propertyMetadata] =
                new ComplexModelDtoResult("John Doe", new ModelValidationNode(propertyMetadata, "theModel.Name"));

            // Attempt to set non-Nullable property to null. BindRequiredAttribute should not be relevant in this
            // case because the binding exists.
            propertyMetadata = dto.PropertyMetadata.Single(o => o.PropertyName == "Age");
            dto.Results[propertyMetadata] =
                new ComplexModelDtoResult(null, new ModelValidationNode(propertyMetadata, "theModel.Age"));

            // Act; must also Validate because null-check error handler is late-bound
            testableBinder.ProcessDto(bindingContext, dto);
            bindingContext.ValidationNode.Validate(validationContext);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(2, modelStateDictionary.Count);

            // Check Name field
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Name", out modelState));
            Assert.Equal(0, modelState.Errors.Count);
            Assert.Equal(ModelValidationState.Valid, modelState.ValidationState);

            // Check Age error.
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            Assert.Equal(1, modelState.Errors.Count);
            Assert.Equal(ModelValidationState.Invalid, modelState.ValidationState);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("A value is required.", modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessDto_RequiredFieldMissing_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithRequired();
            var containerMetadata = GetMetadataForObject(model);
            var bindingContext = CreateContext(containerMetadata);

            // Set no properties though Age (a non-Nullable struct) and City (a class) properties are required.
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(2, modelStateDictionary.Count);

            // Check Age error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.Age", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            var expected = ValidationAttributeUtil.GetRequiredErrorMessage(nameof(ModelWithRequired.Age));
            Assert.Equal(expected, modelError.ErrorMessage);

            // Check City error.
            Assert.True(modelStateDictionary.TryGetValue("theModel.City", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            expected = ValidationAttributeUtil.GetRequiredErrorMessage(nameof(ModelWithRequired.City));
            Assert.Equal(expected, modelError.ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void ProcessDto_RequiredFieldNull_RaisesModelError()
        {
            // Arrange
            var model = new ModelWithRequired();
            var containerMetadata = GetMetadataForObject(model);
            var bindingContext = CreateContext(containerMetadata);

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make Age valid and City invalid.
            var propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == "Age");
            dto.Results[propertyMetadata] =
                new ComplexModelDtoResult(23, new ModelValidationNode(propertyMetadata, "theModel.Age"));
            propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == "City");
            dto.Results[propertyMetadata] =
                new ComplexModelDtoResult(null, new ModelValidationNode(propertyMetadata, "theModel.City"));

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.Count);

            // Check City error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.City", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            var expected = ValidationAttributeUtil.GetRequiredErrorMessage(nameof(ModelWithRequired.City));
            Assert.Equal(expected, modelError.ErrorMessage);
        }

        [Fact]
        public void ProcessDto_RequiredFieldMissing_RaisesModelErrorWithMessage()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForObject(model);
            var bindingContext = CreateContext(containerMetadata);

            // Set no properties though ValueTypeRequired (a non-Nullable struct) property is required.
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.Count);

            // Check ValueTypeRequired error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.ValueTypeRequired", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("Sample message", modelError.ErrorMessage);
        }

        [Fact]
        public void ProcessDto_RequiredFieldNull_RaisesModelErrorWithMessage()
        {
            // Arrange
            var model = new Person();
            var containerMetadata = GetMetadataForObject(model);

            var bindingContext = CreateContext(containerMetadata);

            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);
            var testableBinder = new TestableMutableObjectModelBinder();

            // Make ValueTypeRequired invalid.
            var propertyMetadata = dto.PropertyMetadata.Single(p => p.PropertyName == "ValueTypeRequired");
            dto.Results[propertyMetadata] =
                new ComplexModelDtoResult(null, new ModelValidationNode(propertyMetadata, "theModel.ValueTypeRequired"));

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            var modelStateDictionary = bindingContext.ModelState;
            Assert.Equal(false, modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.Count);

            // Check ValueTypeRequired error.
            ModelState modelState;
            Assert.True(modelStateDictionary.TryGetValue("theModel.ValueTypeRequired", out modelState));
            Assert.Equal(1, modelState.Errors.Count);

            var modelError = modelState.Errors[0];
            Assert.Null(modelError.Exception);
            Assert.NotNull(modelError.ErrorMessage);
            Assert.Equal("Sample message", modelError.ErrorMessage);
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
            var containerMetadata = GetMetadataForObject(model);

            var bindingContext = CreateContext(containerMetadata);
            var dto = new ComplexModelDto(containerMetadata, containerMetadata.Properties);

            var firstNameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "FirstName");
            dto.Results[firstNameProperty] = new ComplexModelDtoResult("John", new ModelValidationNode(firstNameProperty, ""));
            var lastNameProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "LastName");
            dto.Results[lastNameProperty] = new ComplexModelDtoResult("Doe", new ModelValidationNode(lastNameProperty, ""));
            var dobProperty = dto.PropertyMetadata.Single(o => o.PropertyName == "DateOfBirth");
            dto.Results[dobProperty] = null;

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.ProcessDto(bindingContext, dto);

            // Assert
            Assert.Equal("John", model.FirstName);
            Assert.Equal("Doe", model.LastName);
            Assert.Equal(dob, model.DateOfBirth);
            Assert.Equal(true, bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyHasDefaultValue_SetsDefaultValue()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new Person()));

            var propertyMetadata = bindingContext.ModelMetadata.Properties.First(o => o.PropertyName == "PropertyWithDefaultValue");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);
            var requiredValidator = bindingContext.ValidatorProvider
                                                  .GetValidators(propertyMetadata)
                                                  .FirstOrDefault(v => v.IsRequired);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            var person = Assert.IsType<Person>(bindingContext.Model);
            Assert.Equal(123.456m, person.PropertyWithDefaultValue);
            Assert.Equal(true, bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_PropertyIsReadOnly_DoesNothing()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new Person()));
            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "NonUpdateableProperty");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator: null);

            // Assert
            // If didn't throw, success!
        }

        [Fact]
        public void SetProperty_PropertyIsSettable_CallsSetter()
        {
            // Arrange
            var model = new Person();
            var bindingContext = CreateContext(GetMetadataForObject(model));

            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "DateOfBirth");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo");
            var dtoResult = new ComplexModelDtoResult(new DateTime(2001, 1, 1), validationNode);
            var requiredValidator = bindingContext.ValidatorProvider
                                                  .GetValidators(propertyMetadata)
                                                  .FirstOrDefault(v => v.IsRequired);
            var validationContext = new ModelValidationContext(bindingContext, propertyMetadata);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            validationNode.Validate(validationContext);
            Assert.Equal(true, bindingContext.ModelState.IsValid);
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
            var bindingContext = CreateContext(GetMetadataForObject(model));

            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "DateOfDeath");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo");
            var dtoResult = new ComplexModelDtoResult(new DateTime(1800, 1, 1), validationNode);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator: null);

            // Assert
            Assert.Equal("Date of death can't be before date of birth." + Environment.NewLine
                       + "Parameter name: value",
                         bindingContext.ModelState["foo"].Errors[0].Exception.Message);
        }

        [Fact]
        public void SetProperty_SettingNonNullableValueTypeToNull_RequiredValidatorNotPresent_RegistersValidationCallback()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new Person()));
            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "DateOfBirth");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);
            var requiredValidator = GetRequiredValidator(bindingContext, propertyMetadata);
            var validationContext = new ModelValidationContext(bindingContext, propertyMetadata);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            Assert.Equal(true, bindingContext.ModelState.IsValid);
            validationNode.Validate(validationContext, bindingContext.ValidationNode);
            Assert.Equal(false, bindingContext.ModelState.IsValid);
        }

        [Fact]
        public void SetProperty_SettingNonNullableValueTypeToNull_RequiredValidatorPresent_AddsModelError()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new Person()));
            bindingContext.ModelName = " foo";
            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "ValueTypeRequired");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo.ValueTypeRequired");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);
            var requiredValidator = GetRequiredValidator(bindingContext, propertyMetadata);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            Assert.Equal(false, bindingContext.ModelState.IsValid);
            Assert.Equal("Sample message", bindingContext.ModelState["foo.ValueTypeRequired"].Errors[0].ErrorMessage);
        }

        [Fact]
        [ReplaceCulture]
        public void SetProperty_SettingNullableTypeToNull_RequiredValidatorNotPresent_PropertySetterThrows_AddsRequiredMessageString()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new ModelWhosePropertySetterThrows()));
            bindingContext.ModelName = "foo";
            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "NameNoAttribute");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo.NameNoAttribute");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);
            var requiredValidator = GetRequiredValidator(bindingContext, propertyMetadata);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            Assert.Equal(false, bindingContext.ModelState.IsValid);
            Assert.Equal(1, bindingContext.ModelState["foo.NameNoAttribute"].Errors.Count);
            Assert.Equal("This is a different exception." + Environment.NewLine
                       + "Parameter name: value",
                         bindingContext.ModelState["foo.NameNoAttribute"].Errors[0].Exception.Message);
        }

        [Fact]
        public void SetProperty_SettingNullableTypeToNull_RequiredValidatorPresent_PropertySetterThrows_AddsRequiredMessageString()
        {
            // Arrange
            var bindingContext = CreateContext(GetMetadataForObject(new ModelWhosePropertySetterThrows()));
            bindingContext.ModelName = "foo";
            var propertyMetadata = bindingContext.ModelMetadata.Properties.Single(o => o.PropertyName == "Name");
            var validationNode = new ModelValidationNode(propertyMetadata, "foo.Name");
            var dtoResult = new ComplexModelDtoResult(model: null, validationNode: validationNode);
            var requiredValidator = GetRequiredValidator(bindingContext, propertyMetadata);

            var testableBinder = new TestableMutableObjectModelBinder();

            // Act
            testableBinder.SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);

            // Assert
            Assert.Equal(false, bindingContext.ModelState.IsValid);
            Assert.Equal(1, bindingContext.ModelState["foo.Name"].Errors.Count);
            Assert.Equal("This message comes from the [Required] attribute.", bindingContext.ModelState["foo.Name"].Errors[0].ErrorMessage);
        }

        private static ModelBindingContext CreateContext(ModelMetadata metadata)
        {
            var provider = new Mock<IModelValidatorProviderProvider>();
            provider.SetupGet(p => p.ModelValidatorProviders)
                    .Returns(new IModelValidatorProvider[]
                    {
                        new DataAnnotationsModelValidatorProvider(),
                        new DataMemberModelValidatorProvider()
                    });

            return new ModelBindingContext
            {
                ModelState = new ModelStateDictionary(),
                ModelMetadata = metadata,
                ModelName = "theModel",
                ValidatorProvider = new CompositeModelValidatorProvider(provider.Object)
            };
        }

        private static IModelValidator GetRequiredValidator(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
        {
            return bindingContext.ValidatorProvider
                                 .GetValidators(propertyMetadata)
                                 .FirstOrDefault(v => v.IsRequired);
        }

        private static ModelMetadata GetMetadataForCanUpdateProperty(string propertyName)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            return metadataProvider.GetMetadataForProperty(null, typeof(MyModelTestingCanUpdateProperty), propertyName);
        }

        private static ModelMetadata GetMetadataForObject(object o)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            return metadataProvider.GetMetadataForType(() => o, o.GetType());
        }

        private static ModelMetadata GetMetadataForType(Type t)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            return metadataProvider.GetMetadataForType(null, t);
        }

        private static ModelMetadata GetMetadataForParameter(MethodInfo methodInfo, string parameterName)
        {
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            return metadataProvider.GetMetadataForParameter(
                modelAccessor: null, 
                methodInfo: methodInfo, 
                parameterName: parameterName,
                binderMetadata: null);
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
                        throw new ArgumentOutOfRangeException("value", "Date of death can't be before date of birth.");
                    }
                    _dateOfDeath = value;
                }
            }

            [Required(ErrorMessage = "Sample message")]
            public int ValueTypeRequired { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string NonUpdateableProperty { get; private set; }

            [DefaultValue(typeof(decimal), "123.456")]
            public decimal PropertyWithDefaultValue { get; set; }
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
            public string[] ReadOnlyArray { get; private set; }
            public object ReadOnlyObject { get; private set; }
            public string ReadWriteString { get; set; }
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

            public virtual IEnumerable<ModelMetadata> GetMetadataForPropertiesPublic(ModelBindingContext bindingContext)
            {
                return base.GetMetadataForProperties(bindingContext);
            }

            protected override IEnumerable<ModelMetadata> GetMetadataForProperties(ModelBindingContext bindingContext)
            {
                return GetMetadataForPropertiesPublic(bindingContext);
            }

            public virtual void SetPropertyPublic(ModelBindingContext bindingContext, ModelMetadata propertyMetadata, ComplexModelDtoResult dtoResult, IModelValidator requiredValidator)
            {
                base.SetProperty(bindingContext, propertyMetadata, dtoResult, requiredValidator);
            }

            protected override void SetProperty(ModelBindingContext bindingContext, ModelMetadata propertyMetadata, ComplexModelDtoResult dtoResult, IModelValidator requiredValidator)
            {
                SetPropertyPublic(bindingContext, propertyMetadata, dtoResult, requiredValidator);
            }
        }
    }
}
#endif
