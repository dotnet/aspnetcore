// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ActionParameterIntegrationTest
    {
        private class Address
        {
            public string Street { get; set; }
        }

        private class Person3
        {
            public Person3()
            {
                Address = new List<Address>();
            }

            public List<Address> Address { get; }
        }

        [Fact]
        public async Task ActionParameter_NonSettableCollectionModel_EmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "prefix",
                ParameterType = typeof(Person3)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;
            var model = new Person3();

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person3>(modelBindingResult.Model);
            Assert.Single(boundModel.Address);
            Assert.Equal("SomeStreet", boundModel.Address[0].Street);

            // ModelState
            Assert.True(modelState.IsValid);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Address[0].Street", key);
            Assert.Equal("SomeStreet", modelState[key].AttemptedValue);
            Assert.Equal("SomeStreet", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        private class Person6
        {
            public CustomReadOnlyCollection<Address> Address { get; set; }
        }

        [Fact]
        public async Task ActionParameter_ReadOnlyCollectionModel_EmptyPrefix_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "prefix",
                ParameterType = typeof(Person6)
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundModel = Assert.IsType<Person6>(modelBindingResult.Model);
            Assert.NotNull(boundModel);
            Assert.NotNull(boundModel.Address);

            // Read-only collection should not be updated.
            Assert.Empty(boundModel.Address);

            // ModelState (data is can't be validated).
            Assert.False(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Unvalidated, state.ValidationState);
            Assert.Equal("SomeStreet", state.RawValue);
            Assert.Equal("SomeStreet", state.AttemptedValue);
        }

        private class Person4
        {
            public Address[] Address { get; set; }
        }

        [Fact]
        public async Task ActionParameter_SettableArrayModel_EmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "prefix",
                ParameterType = typeof(Person4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;
            var model = new Person4();

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person4>(modelBindingResult.Model);
            Assert.NotNull(boundModel.Address);
            Assert.Single(boundModel.Address);
            Assert.Equal("SomeStreet", boundModel.Address[0].Street);

            // ModelState
            Assert.True(modelState.IsValid);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("Address[0].Street", key);
            Assert.Equal("SomeStreet", modelState[key].AttemptedValue);
            Assert.Equal("SomeStreet", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        private class Person5
        {
            public Address[] Address { get; } = new Address[] { };
        }

        [Fact]
        public async Task ActionParameter_NonSettableArrayModel_EmptyPrefix_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "prefix",
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;
            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person5>(modelBindingResult.Model);
            Assert.NotNull(boundModel.Address);

            // Arrays should not be updated.
            Assert.Empty(boundModel.Address);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public async Task ActionParameter_NonSettableCollectionModel_WithPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Address",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix"
                },
                ParameterType = typeof(Person3)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person3>(modelBindingResult.Model);
            Assert.Single(boundModel.Address);
            Assert.Equal("SomeStreet", boundModel.Address[0].Street);

            // ModelState
            Assert.True(modelState.IsValid);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("prefix.Address[0].Street", key);
            Assert.Equal("SomeStreet", modelState[key].AttemptedValue);
            Assert.Equal("SomeStreet", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task ActionParameter_ReadOnlyCollectionModel_WithPrefix_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Address",
                BindingInfo = new BindingInfo
                {
                    BinderModelName = "prefix"
                },
                ParameterType = typeof(Person6)
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundModel = Assert.IsType<Person6>(modelBindingResult.Model);
            Assert.NotNull(boundModel);
            Assert.NotNull(boundModel.Address);

            // Read-only collection should not be updated.
            Assert.Empty(boundModel.Address);

            // ModelState (data cannot be validated).
            Assert.False(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Unvalidated, state.ValidationState);
            Assert.Equal("SomeStreet", state.AttemptedValue);
            Assert.Equal("SomeStreet", state.RawValue);
        }

        [Fact]
        public async Task ActionParameter_SettableArrayModel_WithPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Address",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix"
                },
                ParameterType = typeof(Person4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person4>(modelBindingResult.Model);
            Assert.Single(boundModel.Address);
            Assert.Equal("SomeStreet", boundModel.Address[0].Street);

            // ModelState
            Assert.True(modelState.IsValid);

            var key = Assert.Single(modelState.Keys);
            Assert.Equal("prefix.Address[0].Street", key);
            Assert.Equal("SomeStreet", modelState[key].AttemptedValue);
            Assert.Equal("SomeStreet", modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task ActionParameter_NonSettableArrayModel_WithPrefix_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Address",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix"
                },
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<Person5>(modelBindingResult.Model);

            // Arrays should not be updated.
            Assert.Empty(boundModel.Address);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        [Fact]
        public async Task ActionParameter_ModelPropertyTypeWithNoParameterlessConstructor_ThrowsException()
        {
            // Arrange
            var parameterType = typeof(Class1);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "p",
                ParameterType = parameterType
            };
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create("Name", "James").Add("Property1.City", "Seattle");
            });
            var modelState = testContext.ModelState;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(
                string.Format(
                    "Could not create an instance of type '{0}'. Model bound complex types must not be abstract or " +
                    "value types and must have a parameterless constructor. Alternatively, set the '{1}' property to" +
                    " a non-null value in the '{2}' constructor.",
                    typeof(ClassWithNoDefaultConstructor).FullName,
                    nameof(Class1.Property1),
                    typeof(Class1).FullName),
                exception.Message);
        }

        [Fact]
        public async Task ActionParameter_BindingToStructModel_ThrowsException()
        {
            // Arrange
            var parameterType = typeof(PointStruct);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                ParameterType = parameterType,
                Name = "p"
            };
            var testContext = ModelBindingTestHelper.GetTestContext();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(
                string.Format(
                    "Could not create an instance of type '{0}'. Model bound complex types must not be abstract or " +
                    "value types and must have a parameterless constructor.",
                    typeof(PointStruct).FullName),
                exception.Message);
        }

        [Theory]
        [InlineData(typeof(ClassWithNoDefaultConstructor))]
        [InlineData(typeof(AbstractClassWithNoDefaultConstructor))]
        public async Task ActionParameter_BindingToTypeWithNoParameterlessConstructor_ThrowsException(Type parameterType)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                ParameterType = parameterType,
                Name = "p"
            };
            var testContext = ModelBindingTestHelper.GetTestContext();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(
                string.Format(
                    "Could not create an instance of type '{0}'. Model bound complex types must not be abstract or " +
                    "value types and must have a parameterless constructor.",
                    parameterType.FullName),
                exception.Message);
        }

        [Fact]
        public async Task ActionParameter_CustomModelBinder_CanCreateModels_ForParameterlessConstructorTypes()
        {
            // Arrange
            var testContext = ModelBindingTestHelper.GetTestContext();
            var modelState = testContext.ModelState;
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(
                testContext.MvcOptions,
                new CustomComplexTypeModelBinderProvider());

            var parameter = new ParameterDescriptor
            {
                Name = "prefix",
                ParameterType = typeof(ClassWithNoDefaultConstructor)
            };

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            var boundModel = Assert.IsType<ClassWithNoDefaultConstructor>(modelBindingResult.Model);
            Assert.Equal(100, boundModel.Id);

            // ModelState
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task ActionParameter_WithBindNever_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = BindingAndValidationController.BindNeverParamInfo.Name,
                ParameterType = typeof(int)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create(parameter.Name, "123");
            });

            var modelState = testContext.ModelState;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForParameter(BindingAndValidationController.BindNeverParamInfo);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);
            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData(123, true)]
        [InlineData(null, false)]
        public async Task ActionParameter_EnforcesBindRequired(int? input, bool isValid)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = BindingAndValidationController.BindRequiredParamInfo.Name,
                ParameterType = typeof(int)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                if (input.HasValue)
                {
                    request.QueryString = QueryString.Create(parameter.Name, input.Value.ToString());
                }
            });

            var modelState = testContext.ModelState;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForParameter(BindingAndValidationController.BindRequiredParamInfo);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.Equal(input.HasValue, modelBindingResult.IsModelSet);
            Assert.Equal(isValid, modelState.IsValid);
            if (isValid)
            {
                Assert.Equal(input.Value, Assert.IsType<int>(modelBindingResult.Model));
            }
        }

        [Theory]
        [InlineData("requiredAndStringLengthParam", null, false)]
        [InlineData("requiredAndStringLengthParam", "", false)]
        [InlineData("requiredAndStringLengthParam", "abc", true)]
        [InlineData("requiredAndStringLengthParam", "abcTooLong", false)]
        [InlineData("displayNameStringLengthParam", null, true)]
        [InlineData("displayNameStringLengthParam", "", true)]
        [InlineData("displayNameStringLengthParam", "abc", true)]
        [InlineData("displayNameStringLengthParam", "abcTooLong", false, "My Display Name")]
        public async Task ActionParameter_EnforcesDataAnnotationsAttributes(
            string paramName,
            string input,
            bool isValid,
            string displayName = null)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameterInfo = BindingAndValidationController.GetParameterInfo(paramName);
            var parameter = new ParameterDescriptor()
            {
                Name = parameterInfo.Name,
                ParameterType = parameterInfo.ParameterType
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                if (input != null)
                {
                    request.QueryString = QueryString.Create(parameter.Name, input);
                }
            });

            var modelState = testContext.ModelState;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForParameter(parameterInfo);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.Equal(input != null, modelBindingResult.IsModelSet);
            Assert.Equal(isValid, modelState.IsValid);
            if (!isValid)
            {
                var entry = modelState[paramName];
                Assert.NotNull(entry);

                var message = entry.Errors.Single().ErrorMessage;
                Assert.Contains(displayName ?? parameter.Name, message);
            }
        }

        [Fact]
        public async Task ActionParameter_CanRunIValidatableObject_EmptyPrefix()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameterInfo = BindingAndValidationController.ValidatableObjectParameterInfo;
            var parameter = new ParameterDescriptor()
            {
                Name = parameterInfo.Name,
                ParameterType = parameterInfo.ParameterType,
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = QueryString.Create(nameof(ModelWithIValidatableObject.FirstName), "Billy");
            });

            var modelState = testContext.ModelState;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForParameter(parameterInfo);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.True(modelBindingResult.IsModelSet, "model is set");
            Assert.False(modelState.IsValid, "model is valid");

            var entry = modelState[string.Empty];
            Assert.NotNull(entry);
            var message = entry.Errors.Single().ErrorMessage;
            Assert.Equal("Not valid.", message);

            entry = modelState[nameof(ModelWithIValidatableObject.FirstName)];
            Assert.NotNull(entry);
            message = entry.Errors.Single().ErrorMessage;
            Assert.Equal("FirstName Not valid.", message);
        }

        [Fact]
        public async Task ActionParameter_CanRunIValidatableObject_WithPrefix()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameterInfo = BindingAndValidationController.ValidatableObjectParameterInfo;
            var parameter = new ParameterDescriptor()
            {
                Name = parameterInfo.Name,
                ParameterType = parameterInfo.ParameterType,
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var key = ModelNames.CreatePropertyModelName(parameter.Name, nameof(ModelWithIValidatableObject.FirstName));
                request.QueryString = QueryString.Create(key, "Billy");
            });

            var modelState = testContext.ModelState;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelMetadata = modelMetadataProvider
                .GetMetadataForParameter(parameterInfo);

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(
                parameter,
                testContext,
                modelMetadataProvider,
                modelMetadata);

            // Assert
            Assert.True(modelBindingResult.IsModelSet, "model is set");
            Assert.False(modelState.IsValid, "model is valid");

            var entry = modelState[parameter.Name];
            Assert.NotNull(entry);
            var message = entry.Errors.Single().ErrorMessage;
            Assert.Equal("Not valid.", message);

            entry = modelState[ModelNames.CreatePropertyModelName(parameter.Name, nameof(ModelWithIValidatableObject.FirstName))];
            Assert.NotNull(entry);
            message = entry.Errors.Single().ErrorMessage;
            Assert.Equal("FirstName Not valid.", message);
        }

        private class ModelWithIValidatableObject : IValidatableObject
        {
            public string FirstName { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                yield return new ValidationResult("Not valid.");
                yield return new ValidationResult("FirstName Not valid.", new string[] { nameof(FirstName) });
            }
        }

        private struct PointStruct
        {
            public PointStruct(double x, double y)
            {
                X = x;
                Y = y;
            }
            public double X { get; }
            public double Y { get; }
        }

        private class Class1
        {
            public ClassWithNoDefaultConstructor Property1 { get; set; }
            public string Name { get; set; }
        }

        private class ClassWithNoDefaultConstructor
        {
            public ClassWithNoDefaultConstructor(int id)
            {
                Id = id;
            }
            public string City { get; set; }
            public int Id { get; }
        }

        private abstract class AbstractClassWithNoDefaultConstructor
        {
            private readonly string _name;

            public AbstractClassWithNoDefaultConstructor()
                : this("James")
            {
            }

            public AbstractClassWithNoDefaultConstructor(string name)
            {
                _name = name;
            }

            public string Name { get; set; }
        }

        private class BindingAndValidationController
        {
            public void MyAction(
                [BindNever] int bindNeverParam,
                [BindRequired] int bindRequiredParam,
                [Required, StringLength(3)] string requiredAndStringLengthParam,
                [Display(Name = "My Display Name"), StringLength(3)] string displayNameStringLengthParam,
                ModelWithIValidatableObject validatableObject)
            {
            }

            private static MethodInfo MyActionMethodInfo
                => typeof(BindingAndValidationController).GetMethod(nameof(MyAction));

            public static ParameterInfo BindNeverParamInfo
                => MyActionMethodInfo.GetParameters()[0];

            public static ParameterInfo BindRequiredParamInfo
                => MyActionMethodInfo.GetParameters()[1];

            public static ParameterInfo ValidatableObjectParameterInfo => MyActionMethodInfo.GetParameters()[4];

            public static ParameterInfo GetParameterInfo(string parameterName)
            {
                return MyActionMethodInfo
                    .GetParameters()
                    .Single(p => p.Name.Equals(parameterName, StringComparison.Ordinal));
            }
        }

        private class CustomReadOnlyCollection<T> : ICollection<T>
        {
            private ICollection<T> _original;

            public CustomReadOnlyCollection()
                : this(new List<T>())
            {
            }

            public CustomReadOnlyCollection(ICollection<T> original)
            {
                _original = original;
            }

            public int Count
            {
                get { return _original.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return _original.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _original.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var t in _original)
                {
                    yield return t;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // By default the ComplexTypeModelBinder fails to construct models for types with no parameterless constructor,
        // but a developer could change this behavior by overriding CreateModel
        private class CustomComplexTypeModelBinder : ComplexTypeModelBinder
        {
            public CustomComplexTypeModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders)
                : base(propertyBinders, NullLoggerFactory.Instance)
            {
            }

            protected override object CreateModel(ModelBindingContext bindingContext)
            {
                Assert.Equal(typeof(ClassWithNoDefaultConstructor), bindingContext.ModelType);
                return new ClassWithNoDefaultConstructor(100);
            }
        }

        private class CustomComplexTypeModelBinderProvider : IModelBinderProvider
        {
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                foreach (var property in context.Metadata.Properties)
                {
                    propertyBinders.Add(property, context.CreateBinder(property));
                }
                return new CustomComplexTypeModelBinder(propertyBinders);
            }
        }
    }
}
