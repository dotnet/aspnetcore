// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    // Integration tests targeting the behavior of the CollectionModelBinder with other model binders.
    //
    // Note that CollectionModelBinder handles both ICollection{T} and IList{T}
    public class CollectionModelBinderIntegrationTest
    {
        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithPrefix_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0]=10&parameter[1]=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
        }

        [Theory]
        [InlineData("?prefix[0]=10&prefix[1]=11")]
        [InlineData("?prefix.index=low&prefix.index=high&prefix[low]=10&prefix[high]=11")]
        [InlineData("?prefix.index=index&prefix.index=indexer&prefix[index]=10&prefix[indexer]=11")]
        [InlineData("?prefix.index=index&prefix.index=indexer&prefix[index]=10&prefix[indexer]=11&prefix[extra]=12")]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithExplicitPrefix_Success(string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData("?[0]=10&[1]=11")]
        [InlineData("?index=low&index=high&[high]=11&[low]=10")]
        [InlineData("?index=index&index=indexer&[indexer]=11&[index]=10")]
        [InlineData("?index=index&index=indexer&[indexer]=11&[index]=10&[extra]=12")]
        public async Task CollectionModelBinder_BindsCollectionOfSimpleType_EmptyPrefix_Success(string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int> { 10, 11 }, model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<int>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person
        {
            public int Id { get; set; }
        }

        [Theory]
        [InlineData("?[0].Id=10&[1].Id=11")]
        [InlineData("?index=low&index=high&[low].Id=10&[high].Id=11")]
        [InlineData("?parameter[0].Id=10&parameter[1].Id=11")]
        [InlineData("?parameter.index=low&parameter.index=high&parameter[low].Id=10&parameter[high].Id=11")]
        [InlineData("?parameter.index=index&parameter.index=indexer&parameter[index].Id=10&parameter[indexer].Id=11")]
        public async Task CollectionModelBinder_BindsListOfComplexType_ImpliedPrefix_Success(string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData("?prefix[0].Id=10&prefix[1].Id=11")]
        [InlineData("?prefix.index=low&prefix.index=high&prefix[high].Id=11&prefix[low].Id=10")]
        [InlineData("?prefix.index=index&prefix.index=indexer&prefix[indexer].Id=11&prefix[index].Id=10")]
        public async Task CollectionModelBinder_BindsListOfComplexType_ExplicitPrefix_Success(string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<Person>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person2
        {
            public int Id { get; set; }

            [BindRequired]
            public string Name { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_WithPrefix_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person2>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?parameter[0].Id=10&parameter[1].Id=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[0].Name);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
            var error = Assert.Single(entry.Errors);
            Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
            error = Assert.Single(entry.Errors);
            Assert.Equal("A value for the 'Name' parameter or property was not provided.", error.ErrorMessage);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_WithExplicitPrefix_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "prefix",
                },
                ParameterType = typeof(List<Person2>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?prefix[0].Id=10&prefix[1].Id=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "prefix[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_WithRequiredProperty_EmptyPrefix_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Person2>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?[0].Id=10&[1].Id=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[1].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[0].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[1].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfSimpleType_WithIndex_Success()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<int>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString =
                    new QueryString("?parameter.index=low&parameter.index=high&parameter[low]=10&parameter[high]=11");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<int>>(modelBindingResult.Model);
            Assert.Equal(new List<int>() { 10, 11 }, model);

            // "index" is not stored in ModelState.
            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[low]").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[high]").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);
            Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_WithRequiredProperty_WithIndex_PartialData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Person2>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?index=low&index=high&[high].Id=11&[low].Id=10");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<List<Person2>>(modelBindingResult.Model);
            Assert.Equal(10, model[0].Id);
            Assert.Null(model[0].Name);
            Assert.Equal(11, model[1].Id);
            Assert.Null(model[1].Name);

            Assert.Equal(4, modelState.Count);
            Assert.Equal(2, modelState.ErrorCount);
            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState, kvp => kvp.Key == "[low].Id").Value;
            Assert.Equal("10", entry.AttemptedValue);
            Assert.Equal("10", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[high].Id").Value;
            Assert.Equal("11", entry.AttemptedValue);
            Assert.Equal("11", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[low].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);

            entry = Assert.Single(modelState, kvp => kvp.Key == "[high].Name").Value;
            Assert.Null(entry.RawValue);
            Assert.Equal(ModelValidationState.Invalid, entry.ValidationState);
        }

        [Fact]
        public async Task CollectionModelBinder_BindsListOfComplexType_WithRequiredProperty_NoData()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(List<Person2>)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString("?");
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.Empty(Assert.IsType<List<Person2>>(modelBindingResult.Model));

            Assert.Empty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
        }

        private class Person4
        {
            public IList<Address4> Addresses { get; set; }
        }

        private class Address4
        {
            public int Zip { get; set; }

            public string Street { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_UsesCustomIndexes()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person4)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var formCollection = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Addresses.index", new [] { "Key1", "Key2" } },
                    { "Addresses[Key1].Street", new [] { "Street1" } },
                    { "Addresses[Key2].Street", new [] { "Street2" } },
                });

                request.Form = formCollection;
                request.ContentType = "application/x-www-form-urlencoded";
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person4>(modelBindingResult.Model);

            Assert.Equal(2, modelState.Count);
            Assert.Equal(0, modelState.ErrorCount);
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState, kvp => kvp.Key == "Addresses[Key1].Street").Value;
            Assert.Equal("Street1", entry.AttemptedValue);
            Assert.Equal("Street1", entry.RawValue);

            entry = Assert.Single(modelState, kvp => kvp.Key == "Addresses[Key2].Street").Value;
            Assert.Equal("Street2", entry.AttemptedValue);
            Assert.Equal("Street2", entry.RawValue);
        }

        private class Person5
        {
            public IList<Address5> Addresses { get; set; }
        }

        private class Address5
        {
            public int Zip { get; set; }

            [StringLength(3)]
            public string Street { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_UsesCustomIndexes_AddsErrorsWithCorrectKeys()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Person5)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                var formCollection = new FormCollection(new Dictionary<string, StringValues>()
                {
                    { "Addresses.index", new [] { "Key1" } },
                    { "Addresses[Key1].Street", new [] { "Street1" } },
                });

                request.Form = formCollection;
                request.ContentType = "application/x-www-form-urlencoded";
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            Assert.IsType<Person5>(modelBindingResult.Model);

            Assert.False(modelState.IsValid);

            var kvp = Assert.Single(modelState);
            Assert.Equal("Addresses[Key1].Street", kvp.Key);
            var entry = kvp.Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal(ValidationAttributeUtil.GetStringLengthErrorMessage(null, 3, "Street"), error.ErrorMessage);
        }

        [Theory]
        [InlineData("?[0].Street=LongStreet")]
        [InlineData("?index=low&[low].Street=LongStreet")]
        [InlineData("?parameter[0].Street=LongStreet")]
        [InlineData("?parameter.index=low&parameter[low].Street=LongStreet")]
        [InlineData("?parameter.index=index&parameter[index].Street=LongStreet")]
        public async Task CollectionModelBinder_BindsCollectionOfComplexType_ImpliedPrefix_FindsValidationErrors(
            string queryString)
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(ICollection<Address5>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.QueryString = new QueryString(queryString);
            });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);
            var model = Assert.IsType<List<Address5>>(modelBindingResult.Model);
            var address = Assert.Single(model);
            Assert.Equal("LongStreet", address.Street);

            Assert.False(modelState.IsValid);

            var entry = Assert.Single(modelState).Value;
            var error = Assert.Single(entry.Errors);
            Assert.Equal(ValidationAttributeUtil.GetStringLengthErrorMessage(null, 3, "Street"), error.ErrorMessage);
        }

        // parameter type, form content, expected type
        public static TheoryData<Type, IDictionary<string, StringValues>, Type> CollectionTypeData
        {
            get
            {
                return new TheoryData<Type, IDictionary<string, StringValues>, Type>
                {
                    {
                        typeof(IEnumerable<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ICollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IReadOnlyCollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IReadOnlyList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(List<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ClosedGenericCollection)
                    },
                    {
                        typeof(ClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ClosedGenericList)
                    },
                    {
                        typeof(ExplicitClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ExplicitClosedGenericCollection)
                    },
                    {
                        typeof(ExplicitClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ExplicitClosedGenericList)
                    },
                    {
                        typeof(ExplicitCollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[0]", new[] { "hello" } },
                            { "[1]", new[] { "world" } },
                        },
                        typeof(ExplicitCollection<string>)
                    },
                    {
                        typeof(ExplicitList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "index", new[] { "low", "high" } },
                            { "[low]", new[] { "hello" } },
                            { "[high]", new[] { "world" } },
                        },
                        typeof(ExplicitList<string>)
                    },
                    {
                        typeof(IEnumerable<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ICollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(IList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(List<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(List<string>)
                    },
                    {
                        typeof(ClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ClosedGenericCollection)
                    },
                    {
                        typeof(ClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ClosedGenericList)
                    },
                    {
                        typeof(ExplicitClosedGenericCollection),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ExplicitClosedGenericCollection)
                    },
                    {
                        typeof(ExplicitClosedGenericList),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ExplicitClosedGenericList)
                    },
                    {
                        typeof(ExplicitCollection<string>),
                        new Dictionary<string, StringValues>
                        {
                            { string.Empty, new[] { "hello", "world" } },
                        },
                        typeof(ExplicitCollection<string>)
                    },
                    {
                        typeof(ExplicitList<string>),
                        new Dictionary<string, StringValues>
                        {
                            { "[]", new[] { "hello", "world" } },
                        },
                        typeof(ExplicitList<string>)
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CollectionTypeData))]
        public async Task CollectionModelBinder_BindsParameterToExpectedType(
            Type parameterType,
            Dictionary<string, StringValues> formContent,
            Type expectedType)
        {
            // Arrange
            var expectedCollection = new List<string> { "hello", "world" };
            var parameter = new ParameterDescriptor
            {
                Name = "parameter",
                ParameterType = parameterType,
            };

            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                request.Form = new FormCollection(formContent);
            });
            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            Assert.IsType(expectedType, modelBindingResult.Model);

            var model = modelBindingResult.Model as IEnumerable<string>;
            Assert.NotNull(model); // Guard
            Assert.Equal(expectedCollection, model);

            Assert.True(modelState.IsValid);
            Assert.NotEmpty(modelState);
            Assert.Equal(0, modelState.ErrorCount);
        }

        // Regression test for #7052
        [Fact]
        public async Task CollectionModelBinder_ThrowsOn1025Items_AtTopLevel()
        {
            // Arrange
            var expectedMessage = $"Collection bound to 'parameter' exceeded " +
                $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} (1024). This limit is a " +
                $"safeguard against incorrect model binders and models. Address issues in " +
                $"'{typeof(SuccessfulModel)}'. For example, this type may have a property with a model binder that " +
                $"always succeeds. See the {nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} " +
                $"documentation for more information.";
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(IList<SuccessfulModel>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // CollectionModelBinder binds an empty collection when value providers are all empty.
                request.QueryString = new QueryString("?a=b");
            });

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(expectedMessage, exception.Message);
        }

        // Ensure CollectionModelBinder allows MaxModelBindingCollectionSize items.
        [Fact]
        public async Task CollectionModelBinder_Binds3Items_WithIndices()
        {
            // Arrange
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(IList<SuccessfulModel>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?Index=0&Index=1&Index=2");
                },
                options => options.MaxModelBindingCollectionSize = 3);

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelState.IsValid);
            Assert.Equal(0, modelState.ErrorCount);

            Assert.True(result.IsModelSet);
            var locations = Assert.IsType<List<SuccessfulModel>>(result.Model);
            Assert.Collection(
                locations,
                item =>
                {
                    Assert.True(item.IsBound);
                    Assert.Null(item.Name);
                },
                item =>
                {
                    Assert.True(item.IsBound);
                    Assert.Null(item.Name);
                },
                item =>
                {
                    Assert.True(item.IsBound);
                    Assert.Null(item.Name);
                });
        }

        // Ensure CollectionModelBinder disallows one more than MaxModelBindingCollectionSize items.
        [Fact]
        public async Task CollectionModelBinder_ThrowsOn4Items_WithIndices()
        {
            // Arrange
            var expectedMessage = $"Collection bound to 'parameter' exceeded " +
                $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} (3). This limit is a " +
                $"safeguard against incorrect model binders and models. Address issues in " +
                $"'{typeof(SuccessfulModel)}'. For example, this type may have a property with a model binder that " +
                $"always succeeds. See the {nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} " +
                $"documentation for more information.";
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(IList<SuccessfulModel>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?Index=0&Index=1&Index=2&Index=3");
                },
                options => options.MaxModelBindingCollectionSize = 3);

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(expectedMessage, exception.Message);
        }

        private class SuccessfulContainer
        {
            public IList<SuccessfulModel> Successes { get; set; }
        }

        [Fact]
        public async Task CollectionModelBinder_ThrowsOn1025Items()
        {
            // Arrange
            var expectedMessage = $"Collection bound to 'Successes' exceeded " +
                $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} (1024). This limit is a " +
                $"safeguard against incorrect model binders and models. Address issues in " +
                $"'{typeof(SuccessfulModel)}'. For example, this type may have a property with a model binder that " +
                $"always succeeds. See the {nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} " +
                $"documentation for more information.";
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(SuccessfulContainer),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(request =>
            {
                // CollectionModelBinder binds an empty collection when value providers lack matching data.
                request.QueryString = new QueryString("?Successes[0]=b");
            });

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => parameterBinder.BindModelAsync(parameter, testContext));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task CollectionModelBinder_CollectionOfSimpleTypes_DoesNotResultInValidationError()
        {
            // Regression test for https://github.com/dotnet/aspnetcore/issues/13512
            // Arrange
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Collection<string>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?[0]=hello&[1]=");
                });

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelState.IsValid);
            Assert.Equal(0, modelState.ErrorCount);

            Assert.True(result.IsModelSet);
            var model = Assert.IsType<Collection<string>>(result.Model);
            Assert.Collection(
                model,
                item => Assert.Equal("hello", item),
                item => Assert.Null(item));

            Assert.Collection(
                modelState,
                kvp =>
                {
                    Assert.Equal("[0]", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
                },
                kvp =>
                {
                    Assert.Equal("[1]", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
                });
        }

        [Fact]
        public async Task CollectionModelBinder_CollectionOfNonNullableTypes_AppliesImplicitRequired()
        {
            // Arrange
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Collection<string>),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString("?[0]=hello&[1]=");
                });

            var modelState = testContext.ModelState;
            var metadata = testContext.MetadataProvider.GetMetadataForType(parameter.ParameterType);
            var valueProvider = await CompositeValueProvider.CreateAsync(testContext);
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext);

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelState.IsValid);
            Assert.Equal(0, modelState.ErrorCount);

            Assert.True(result.IsModelSet);
            var model = Assert.IsType<Collection<string>>(result.Model);
            Assert.Collection(
                model,
                item => Assert.Equal("hello", item),
                item => Assert.Null(item));

            Assert.Collection(
                modelState,
                kvp =>
                {
                    Assert.Equal("[0]", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
                },
                kvp =>
                {
                    Assert.Equal("[1]", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
                });
        }

        private class ClosedGenericCollection : Collection<string>
        {
        }

        private class ClosedGenericList : List<string>
        {
        }

        private class ExplicitClosedGenericCollection : ICollection<string>
        {
            private readonly List<string> _data = new List<string>();

            int ICollection<string>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<string>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<string>.Add(string item)
            {
                _data.Add(item);
            }

            void ICollection<string>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<string>.Contains(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitClosedGenericList : IList<string>
        {
            private readonly List<string> _data = new List<string>();

            string IList<string>.this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            int ICollection<string>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<string>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<string>.Add(string item)
            {
                _data.Add(item);
            }

            void ICollection<string>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<string>.Contains(string item)
            {
                throw new NotImplementedException();
            }

            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            int IList<string>.IndexOf(string item)
            {
                throw new NotImplementedException();
            }

            void IList<string>.Insert(int index, string item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotImplementedException();
            }

            void IList<string>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitCollection<T> : ICollection<T>
        {
            private readonly List<T> _data = new List<T>();

            int ICollection<T>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<T>.Add(T item)
            {
                _data.Add(item);
            }

            void ICollection<T>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotImplementedException();
            }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw new NotImplementedException();
            }
        }

        private class ExplicitList<T> : IList<T>
        {
            private readonly List<T> _data = new List<T>();

            T IList<T>.this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            int ICollection<T>.Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            void ICollection<T>.Add(T item)
            {
                _data.Add(item);
            }

            void ICollection<T>.Clear()
            {
                _data.Clear();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotImplementedException();
            }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_data).GetEnumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            int IList<T>.IndexOf(T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.Insert(int index, T item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw new NotImplementedException();
            }

            void IList<T>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }
        }
    }
}
