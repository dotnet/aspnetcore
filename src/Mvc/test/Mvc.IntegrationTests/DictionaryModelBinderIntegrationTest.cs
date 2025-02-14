// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

// Integration tests targeting the behavior of the DictionaryModelBinder with other model binders.
public class DictionaryModelBinderIntegrationTest
{
    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithPrefixAndKVP_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[0].Key=key0&parameter[0].Value=10");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model);

        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Key").Value;
        Assert.Equal("key0", entry.AttemptedValue);
        Assert.Equal("key0", entry.RawValue);

        entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[0].Value").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithPrefixAndItem_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[key0]=10");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model);

        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var kvp = Assert.Single(modelState);
        Assert.Equal("parameter[key0]", kvp.Key);
        var entry = kvp.Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithIndex_Success()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString =
                new QueryString("?parameter.index=low&parameter[low].Key=key0&parameter[low].Value=10");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, int>() { { "key0", 10 } }, model);

        // "index" is not stored in ModelState.
        Assert.Equal(2, modelState.Count);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[low].Key").Value;
        Assert.Equal("key0", entry.AttemptedValue);
        Assert.Equal("key0", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);

        entry = Assert.Single(modelState, kvp => kvp.Key == "parameter[low].Value").Value;
        Assert.Equal("10", entry.AttemptedValue);
        Assert.Equal("10", entry.RawValue);
        Assert.Equal(ModelValidationState.Valid, entry.ValidationState);
    }

    [Theory]
    [InlineData("?prefix[key0]=10")]
    [InlineData("?prefix[0].Key=key0&prefix[0].Value=10")]
    [InlineData("?prefix.index=low&prefix[low].Key=key0&prefix[low].Value=10")]
    [InlineData("?prefix.index=index&prefix[index].Key=key0&prefix[index].Value=10")]
    [InlineData("?prefix.index=index&prefix[index].Key=key0&prefix[index].Value=10&prefix[extra].Key=key4&prefix[extra].Value=5")]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_WithExplicitPrefix_Success(
        string queryString)
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
            ParameterType = typeof(Dictionary<string, int>)
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

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, int>() { { "key0", 10 }, }, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[key0]=10")]
    [InlineData("?[0].Key=key0&[0].Value=10")]
    [InlineData("?index=low&[low].Key=key0&[low].Value=10")]
    [InlineData("?index=index&[index].Key=key0&[index].Value=10")]
    [InlineData("?index=index&[index].Key=key0&[index].Value=10&[extra].Key=key4&[extra].Value=5")]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_EmptyPrefix_Success(string queryString)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>)
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

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, int>() { { "key0", 10 }, }, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    public static TheoryData<Action<HttpRequest>> ThreeEntryTestData
    {
        get
        {
            var impliedPrefixQueryString = "?parameter[archive]=1&parameter[correlation]=2&parameter[index]=3";
            var noPrefixQueryString = "?[archive]=1&[correlation]=2&[index]=3";
            var reversedNoPrefixQueryString = "?[index]=3&[correlation]=2&[archive]=1";
            var impliedPrefixDictionary = new Dictionary<string, StringValues>
                {
                    { "parameter[archive]", "1" },
                    { "parameter[correlation]", "2" },
                    { "parameter[index]", "3" },
                };
            var reversedImpliedPrefixDictionary = new Dictionary<string, StringValues>
                {
                    { "parameter[index]", "3" },
                    { "parameter[correlation]", "2" },
                    { "parameter[archive]", "1" },
                };
            var longFormDictionary = new Dictionary<string, StringValues>
                {
                    { "parameter[0].Key", "archive" },
                    { "parameter[0].Value", "1" },
                    { "parameter[1].Key", "correlation" },
                    { "parameter[1].Value", "2" },
                    { "parameter[2].Key", "index" },
                    { "parameter[2].Value", "3" },
                };
            var longerFormDictionary = new Dictionary<string, StringValues>
                {
                    { "parameter[indexer].Key", "archive" },
                    { "parameter[indexer].Value", "1" },
                    { "parameter[index].Key", "correlation" },
                    { "parameter.index", new[] { "indexer", "index", "indexes" } },
                    { "parameter[index].Value", "2" },
                    { "parameter[indexes].Key", "index" },
                    { "parameter[indexes].Value", "3" },
                };
            var longestFormDictionary = new Dictionary<string, StringValues>
                {
                    { "parameter[indexer].Key", "archive" },
                    { "parameter[indexer].Value", "1" },
                    { "parameter[index].Key", "correlation" },
                    { "parameter[extra].Key", "index" },
                    { "parameter[extra].Value", "4" },
                    { "parameter.index", new[] { "indexer", "index", "indexes" } },
                    { "parameter[index].Value", "2" },
                    { "parameter[indexes].Key", "index" },
                    { "parameter[indexes].Value", "3" },
                    { "parameter[another].Key", "index" },
                    { "parameter[another].Value", "5" },
                };
            var noPrefixDictionary = new Dictionary<string, StringValues>
                {
                    { "[archive]", "1" },
                    { "[correlation]", "2" },
                    { "[index]", "3" },
                };
            var reversedNoPrefixDictionary = new Dictionary<string, StringValues>
                {
                    { "[index]", "3" },
                    { "[correlation]", "2" },
                    { "[archive]", "1" },
                };

            return new TheoryData<Action<HttpRequest>>
                {
                    request => request.QueryString = new QueryString(impliedPrefixQueryString),
                    request => request.QueryString = new QueryString(noPrefixQueryString),
                    request => request.QueryString = new QueryString(reversedNoPrefixQueryString),
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(impliedPrefixDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(reversedImpliedPrefixDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(longFormDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(longerFormDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(longestFormDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(noPrefixDictionary);
                    },
                    request =>
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Form = new FormCollection(reversedNoPrefixDictionary);
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ThreeEntryTestData))]
    public async Task DictionaryModelBinder_Binds3EntriesOfSimpleType(Action<HttpRequest> updateRequest)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, int>
            {
                { "archive", 1 },
                { "correlation", 2 },
                { "index", 3 },
            };

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var testContext = ModelBindingTestHelper.GetTestContext(updateRequest);
        var modelState = testContext.ModelState;
        var parameter = new ParameterDescriptor
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>),
        };

        // Act
        var result = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(result.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(result.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [MemberData(nameof(ThreeEntryTestData))]
    public async Task DictionaryModelBinder_Binds3EntriesOfSimpleType_WithJQueryQueryString(
        Action<HttpRequest> updateRequest)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, int>
            {
                { "archive", 1 },
                { "correlation", 2 },
                { "index", 3 },
            };

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var testContext = ModelBindingTestHelper.GetTestContext(
            updateRequest,
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));
        var modelState = testContext.ModelState;
        var parameter = new ParameterDescriptor
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>),
        };

        // Act
        var result = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(result.IsModelSet);

        var model = Assert.IsType<Dictionary<string, int>>(result.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_NoData()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>)
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

        var model = Assert.IsType<Dictionary<string, int>>(modelBindingResult.Model);
        Assert.Empty(model);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleType_NoData_WithDefaultValue()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameterInfo = typeof(DictionaryModelBinderIntegrationTest)
            .GetMethod(nameof(SampleMethod_SimpleType), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        var parameter = new Controllers.ControllerParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, int>),
            ParameterInfo = parameterInfo
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.False(modelBindingResult.IsModelSet);
        Assert.Null(modelBindingResult.Model);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    private class Person
    {
        [Range(minimum: 0, maximum: 15, ErrorMessage = "You're out of range.")]
        public int Id { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Person other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{ { Id } }}";
        }
    }

    public static TheoryData<string> ComplexType_ImpliedPrefixData
    {
        get
        {
            return new TheoryData<string>
                {
                    "?[key0].Id=10",
                    "?[0].Key=key0&[0].Value.Id=10",
                    "?index=low&[low].Key=key0&[low].Value.Id=10",
                    "?parameter[key0].Id=10",
                    "?parameter[0].Key=key0&parameter[0].Value.Id=10",
                    "?parameter.index=low&parameter[low].Key=key0&parameter[low].Value.Id=10",
                    "?parameter.index=index&parameter[index].Key=key0&parameter[index].Value.Id=10",
                };
        }
    }

    [Theory]
    [MemberData(nameof(ComplexType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithImpliedPrefix(string queryString)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
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

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, Person> { { "key0", new Person { Id = 10 } }, }, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[key0][Id]=10")] // Both key segments will be rewritten.
    [InlineData("?[0][Key]=key0&[0][Value][Id]=10")]
    [InlineData("?parameter[key0][Id]=10")]
    [InlineData("?parameter[0][Key]=key0&parameter[0][Value][Id]=10")]
    [MemberData(nameof(ComplexType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithImpliedPrefixAndJQuery(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, Person> { { "key0", new Person { Id = 10 } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory after default factories.
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[key0][Id]=10")] // Both key segments will be rewritten.
    [InlineData("?[0][Key]=key0&[0][Value][Id]=10")]
    [InlineData("?parameter[key0][Id]=10")]
    [InlineData("?parameter[0][Key]=key0&parameter[0][Value][Id]=10")]
    [MemberData(nameof(ComplexType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithImpliedPrefixAndJQueryFirst(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, Person> { { "key0", new Person { Id = 10 } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory before default factories.
            options => options.ValueProviderFactories.Insert(0, new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[42][Id]=10")] // Only Id segment will be rewritten.
    [InlineData("?parameter[42][Id]=10")]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithImpliedPrefixIntegralKeysAndJQuery(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, Person> { { "42", new Person { Id = 10 } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory after default factories.
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[42][Id]=10")] // Only Id segment will be rewritten.
    [InlineData("?parameter[42][Id]=10")]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithImpliedPrefixIntegralKeysAndJQueryFirst(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, Person> { { "42", new Person { Id = 10 } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory before default factories.
            options => options.ValueProviderFactories.Insert(0, new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    public static TheoryData<string> ComplexType_ExplicitPrefixData
    {
        get
        {
            return new TheoryData<string>
                {
                    "?prefix[key0].Id=10",
                    "?prefix[0].Key=key0&prefix[0].Value.Id=10",
                    "?prefix.index=low&prefix[low].Key=key0&prefix[low].Value.Id=10",
                    "?prefix.index=index&prefix[index].Key=key0&prefix[index].Value.Id=10",
                };
        }
    }

    [Theory]
    [MemberData(nameof(ComplexType_ExplicitPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithExplicitPrefix(
        string queryString)
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
            ParameterType = typeof(Dictionary<string, Person>)
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

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, Person> { { "key0", new Person { Id = 10 } }, }, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?prefix[key0][Id]=10")]
    [MemberData(nameof(ComplexType_ExplicitPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_WithExplicitPrefixAndJQuery(
        string queryString)
    {
        // Arrange
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory after default factories.
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "prefix",
            },
            ParameterType = typeof(Dictionary<string, Person>)
        };

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, Person> { { "key0", new Person { Id = 10 } }, }, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[key0].Id=100")]
    [InlineData("?[0].Key=key0&[0].Value.Id=100")]
    [InlineData("?index=low&[low].Key=key0&[low].Value.Id=100")]
    [InlineData("?parameter[key0].Id=100")]
    [InlineData("?parameter[0].Key=key0&parameter[0].Value.Id=100")]
    [InlineData("?parameter.index=low&parameter[low].Key=key0&parameter[low].Value.Id=100")]
    [InlineData("?parameter.index=index&parameter[index].Key=key0&parameter[index].Value.Id=100")]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_ImpliedPrefix_FindsValidationErrors(
        string queryString)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
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

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Equal(new Dictionary<string, Person> { { "key0", new Person { Id = 100 } }, }, model);

        Assert.NotEmpty(modelState);
        Assert.False(modelState.IsValid);
        Assert.All(modelState, kvp =>
        {
            Assert.NotEqual(ModelValidationState.Unvalidated, kvp.Value.ValidationState);
            Assert.NotEqual(ModelValidationState.Skipped, kvp.Value.ValidationState);
        });

        var entry = Assert.Single(modelState, kvp => kvp.Value.ValidationState == ModelValidationState.Invalid);
        var error = Assert.Single(entry.Value.Errors);
        Assert.Equal("You're out of range.", error.ErrorMessage);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_NoData()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>)
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

        var model = Assert.IsType<Dictionary<string, Person>>(modelBindingResult.Model);
        Assert.Empty(model);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfComplexType_NoData_WithDefaultValue()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameterInfo = typeof(DictionaryModelBinderIntegrationTest)
            .GetMethod(nameof(SampleMethod_ComplexType), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        var parameter = new Controllers.ControllerParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, Person>),
            ParameterInfo = parameterInfo
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.False(modelBindingResult.IsModelSet);
        Assert.Null(modelBindingResult.Model);

        Assert.Empty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    public static TheoryData<string> CollectionType_ImpliedPrefixData
    {
        get
        {
            return new TheoryData<string>
                {
                    "?[key0]=10&[key0]=11",
                    "?[key0][0]=10&[key0][1]=11",
                    "?[0].Key=key0&[0].Value[0]=10&[0].Value[1]=11",
                    "?index=low&[low].Key=key0&[low].Value[0]=10&[low].Value[1]=11",
                    "?parameter[key0]=10&parameter[key0]=11",
                    "?parameter[key0][0]=10&parameter[key0][1]=11",
                    "?parameter[0].Key=key0&parameter[0].Value[0]=10&parameter[0].Value[1]=11",
                    "?parameter.index=low&parameter[low].Key=key0&parameter[low].Value[0]=10&parameter[low].Value[1]=11",
                    "?parameter.index=index&parameter[index].Key=key0&parameter[index].Value[0]=10&parameter[index].Value[1]=11",
                };
        }
    }

    [Theory]
    [MemberData(nameof(CollectionType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfCollectionType_WithImpliedPrefix(string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string[]> { { "key0", new[] { "10", "11" } } };
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string[]>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString));
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string[]>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [MemberData(nameof(CollectionType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfCollectionType_WithImpliedPrefixAndJQuery(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string[]> { { "key0", new[] { "10", "11" } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory after default factories.
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string[]>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string[]>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [MemberData(nameof(CollectionType_ImpliedPrefixData))]
    public async Task DictionaryModelBinder_BindsDictionaryOfCollectionType_WithImpliedPrefixAndJQueryFirst(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string[]> { { "key0", new[] { "10", "11" } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory before default factories.
            options => options.ValueProviderFactories.Insert(0, new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string[]>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string[]>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[42]=10&[42]=11")]
    [InlineData("?[42][]=10&[42][]=11")]
    [InlineData("?[42][0]=10&[42][1]=11")]
    [InlineData("?parameter[42]=10&parameter[42]=11")]
    [InlineData("?parameter[42][]=10&parameter[42][]=11")]
    [InlineData("?parameter[42][0]=10&parameter[42][1]=11")]
    public async Task DictionaryModelBinder_BindsDictionaryOfCollectionType_WithImpliedPrefixIntegralKeysAndJQuery(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string[]> { { "42", new[] { "10", "11" } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory after default factories.
            options => options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string[]>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string[]>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    [Theory]
    [InlineData("?[42]=10&[42]=11")]
    [InlineData("?[42][]=10&[42][]=11")]
    [InlineData("?[42][0]=10&[42][1]=11")]
    [InlineData("?parameter[42]=10&parameter[42]=11")]
    [InlineData("?parameter[42][]=10&parameter[42][]=11")]
    [InlineData("?parameter[42][0]=10&parameter[42][1]=11")]
    public async Task DictionaryModelBinder_BindsDictionaryOfCollectionType_WithImpliedPrefixIntegralKeysAndJQueryFirst(
        string queryString)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string[]> { { "42", new[] { "10", "11" } } };
        var testContext = ModelBindingTestHelper.GetTestContext(
            request => request.QueryString = new QueryString(queryString),
            // Add JQueryQueryStringValueProviderFactory before default factories.
            options => options.ValueProviderFactories.Insert(0, new JQueryQueryStringValueProviderFactory()));

        var modelState = testContext.ModelState;
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(testContext.HttpContext.RequestServices);
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string[]>)
        };

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string[]>>(modelBindingResult.Model);
        Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

    // parameter type, query string, expected type
    public static TheoryData<Type, string, Type> DictionaryTypeData
    {
        get
        {
            return new TheoryData<Type, string, Type>
                {
                    {
                        typeof(IDictionary<string, string>),
                        "?[key0]=hello&[key1]=world",
                        typeof(Dictionary<string, string>)
                    },
                    {
                        typeof(Dictionary<string, string>),
                        "?[key0]=hello&[key1]=world",
                        typeof(Dictionary<string, string>)
                    },
                    {
                        typeof(ClosedGenericDictionary),
                        "?[key0]=hello&[key1]=world",
                        typeof(ClosedGenericDictionary)
                    },
                    {
                        typeof(ClosedGenericKeyDictionary<string>),
                        "?[key0]=hello&[key1]=world",
                        typeof(ClosedGenericKeyDictionary<string>)
                    },
                    {
                        typeof(ExplicitClosedGenericDictionary),
                        "?[key0]=hello&[key1]=world",
                        typeof(ExplicitClosedGenericDictionary)
                    },
                    {
                        typeof(ExplicitDictionary<string, string>),
                        "?[key0]=hello&[key1]=world",
                        typeof(ExplicitDictionary<string, string>)
                    },
                    {
                        typeof(IDictionary<string, string>),
                        "?index=low&index=high&[low].Key=key0&[low].Value=hello&[high].Key=key1&[high].Value=world",
                        typeof(Dictionary<string, string>)
                    },
                    {
                        typeof(Dictionary<string, string>),
                        "?[0].Key=key0&[0].Value=hello&[1].Key=key1&[1].Value=world",
                        typeof(Dictionary<string, string>)
                    },
                    {
                        typeof(ClosedGenericDictionary),
                        "?index=low&index=high&[low].Key=key0&[low].Value=hello&[high].Key=key1&[high].Value=world",
                        typeof(ClosedGenericDictionary)
                    },
                    {
                        typeof(ClosedGenericKeyDictionary<string>),
                        "?[0].Key=key0&[0].Value=hello&[1].Key=key1&[1].Value=world",
                        typeof(ClosedGenericKeyDictionary<string>)
                    },
                    {
                        typeof(ExplicitClosedGenericDictionary),
                        "?index=low&index=high&[low].Key=key0&[low].Value=hello&[high].Key=key1&[high].Value=world",
                        typeof(ExplicitClosedGenericDictionary)
                    },
                    {
                        typeof(ExplicitDictionary<string, string>),
                        "?[0].Key=key0&[0].Value=hello&[1].Key=key1&[1].Value=world",
                        typeof(ExplicitDictionary<string, string>)
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DictionaryTypeData))]
    public async Task DictionaryModelBinder_BindsParameterToExpectedType(
        Type parameterType,
        string queryString,
        Type expectedType)
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, string>
            {
                { "key0", "hello" },
                { "key1", "world" },
            };
        var parameter = new ParameterDescriptor
        {
            Name = "parameter",
            ParameterType = parameterType,
        };

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString(queryString);
        });
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        Assert.IsType(expectedType, modelBindingResult.Model);

        var model = modelBindingResult.Model as IDictionary<string, string>;
        Assert.NotNull(model); // Guard
        Assert.Equal(expectedDictionary.Keys, model.Keys);
        Assert.Equal(expectedDictionary.Values, model.Values);

        Assert.True(modelState.IsValid);
        Assert.NotEmpty(modelState);
        Assert.Equal(0, modelState.ErrorCount);
    }

    [Fact]
    public async Task DictionaryModelBinder_ThrowsOn1025Items_AtTopLevel()
    {
        // Arrange
        var expectedMessage = $"Collection bound to 'parameter' exceeded " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} (1024). This limit is a " +
            $"safeguard against incorrect model binders and models. Address issues in " +
            $"'{typeof(KeyValuePair<SuccessfulModel, SuccessfulModel>)}'. For example, this type may have a " +
            $"property with a model binder that always succeeds. See the " +
            $"{nameof(MvcOptions)}.{nameof(MvcOptions.MaxModelBindingCollectionSize)} documentation for more " +
            $"information.";
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<SuccessfulModel, SuccessfulModel>),
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

    [Fact]
    public async Task DictionaryModelBinder_DictionaryOfSimpleType_NullValue_DoesNotResultInRequiredValidation()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/13512
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, string>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[key0]=");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, string>>(modelBindingResult.Model);
        Assert.Collection(
            model.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("key0", kvp.Key);
                Assert.Null(kvp.Value);
            });

        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("parameter[key0]", kvp.Key);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            });
        Assert.Equal(0, modelState.ErrorCount);
        Assert.True(modelState.IsValid);
    }

#nullable enable
    public class NonNullPerson
    {
        public int Age { get; set; }

        // This should be implicitly required
        public string Name { get; set; } = default!;
    }
#nullable restore

    [Fact]
    public async Task DictionaryModelBinder_ValuesIsNonNullableType_AppliesImplicitRequired()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, NonNullPerson>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[key0].Age=&parameter[key0].Name=name0&parameter[key1].Age=27&parameter[key1].Name=");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, NonNullPerson>>(modelBindingResult.Model);
        Assert.Collection(
            model.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("key0", kvp.Key);
                var person = kvp.Value;
                Assert.Equal(0, person.Age);
                Assert.Equal("name0", person.Name);
            },
            kvp =>
            {
                Assert.Equal("key1", kvp.Key);
                var person = kvp.Value;
                Assert.Equal(27, person.Age);
                Assert.Null(person.Name);
            });

        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("parameter[key0].Age", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                Assert.Equal("The value '' is invalid.", Assert.Single(kvp.Value.Errors).ErrorMessage);
            },
            kvp =>
            {
                Assert.Equal("parameter[key0].Name", kvp.Key);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("parameter[key1].Age", kvp.Key);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("parameter[key1].Name", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                Assert.Equal("The Name field is required.", Assert.Single(kvp.Value.Errors).ErrorMessage);
            });
        Assert.Equal(2, modelState.ErrorCount);
        Assert.False(modelState.IsValid);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleValueAndEnumKey_WithError()
    {
        // Arrange
        var expectedDictionary = new Dictionary<DayOfWeek, string>
        {
            { DayOfWeek.Monday, "hello" },
            { DayOfWeek.Tuesday, "world" },
        };
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<DayOfWeek, string>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[Monday]=hello&parameter[Tuesday]=world&parameter[Invalid]=exclamation");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.False(modelBindingResult.IsModelSet);

        Assert.NotEmpty(modelState);
        Assert.Equal(1, modelState.ErrorCount);
        Assert.False(modelState.IsValid);
        Assert.Equal("Invalid is not a valid value for DayOfWeek.", modelState["parameter"].Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task DictionaryModelBinder_BindsDictionaryOfSimpleValueAndEnumValue_WithError()
    {
        // Arrange
        var expectedDictionary = new Dictionary<string, DayOfWeek>
        {
            { "hello", DayOfWeek.Monday },
            { "world", DayOfWeek.Tuesday },
            { "exclamation", DayOfWeek.Sunday },
        };
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, DayOfWeek>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[hello]=Monday&parameter[world]=Tuesday&parameter[exclamation]=BadEnumValue");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);
        var model = Assert.IsType<Dictionary<string, DayOfWeek>>(modelBindingResult.Model);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);
        // Assert.Equal(expectedDictionary, model);

        Assert.NotEmpty(modelState);
        Assert.Equal(1, modelState.ErrorCount);
        Assert.False(modelState.IsValid);
        Assert.Equal("The value 'BadEnumValue' is not valid for Value.", modelState["parameter[exclamation]"].Errors[0].ErrorMessage);
    }

#nullable enable
    public class NonNullPersonWithRequiredProperties
    {
        public int Age { get; set; }

        [Required]
        public string? Name { get; set; }
    }
#nullable restore

    [Fact]
    public async Task DictionaryModelBinder_ValuesNullableTypeWithRequiredAttributes_AppliesValidation()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "parameter",
            ParameterType = typeof(Dictionary<string, NonNullPersonWithRequiredProperties>)
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.QueryString = new QueryString("?parameter[key0].Age=&parameter[key0].Name=name0&parameter[key1].Age=27&parameter[key1].Name=");
        });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        Assert.True(modelBindingResult.IsModelSet);

        var model = Assert.IsType<Dictionary<string, NonNullPersonWithRequiredProperties>>(modelBindingResult.Model);
        Assert.Collection(
            model.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("key0", kvp.Key);
                var person = kvp.Value;
                Assert.Equal(0, person.Age);
                Assert.Equal("name0", person.Name);
            },
            kvp =>
            {
                Assert.Equal("key1", kvp.Key);
                var person = kvp.Value;
                Assert.Equal(27, person.Age);
                Assert.Null(person.Name);
            });

        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("parameter[key0].Age", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                Assert.Equal("The value '' is invalid.", Assert.Single(kvp.Value.Errors).ErrorMessage);
            },
            kvp =>
            {
                Assert.Equal("parameter[key0].Name", kvp.Key);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("parameter[key1].Age", kvp.Key);
                Assert.Equal(ModelValidationState.Valid, kvp.Value.ValidationState);
            },
            kvp =>
            {
                Assert.Equal("parameter[key1].Name", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);
                Assert.Equal("The Name field is required.", Assert.Single(kvp.Value.Errors).ErrorMessage);
            });
        Assert.Equal(2, modelState.ErrorCount);
        Assert.False(modelState.IsValid);
    }

    private class ClosedGenericDictionary : Dictionary<string, string>
    {
    }

    private class ClosedGenericKeyDictionary<TValue> : Dictionary<string, TValue>
    {
    }

    private class ExplicitClosedGenericDictionary : IDictionary<string, string>
    {
        private readonly IDictionary<string, string> _data = new Dictionary<string, string>();

        string IDictionary<string, string>.this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                _data[key] = value;
            }
        }

        int ICollection<KeyValuePair<string, string>>.Count
        {
            get
            {
                return _data.Count;
            }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        ICollection<string> IDictionary<string, string>.Keys
        {
            get
            {
                return _data.Keys;
            }
        }

        ICollection<string> IDictionary<string, string>.Values
        {
            get
            {
                return _data.Values;
            }
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            _data.Add(item);
        }

        void IDictionary<string, string>.Add(string key, string value)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, string>>.Clear()
        {
            _data.Clear();
        }

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, string>.ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, string>.Remove(string key)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, string>.TryGetValue(string key, out string value)
        {
            return _data.TryGetValue(key, out value);
        }
    }

    private class ExplicitDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _data = new Dictionary<TKey, TValue>();

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                _data[key] = value;
            }
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get
            {
                return _data.Count;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return _data.Keys;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return _data.Values;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            _data.Add(item);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            _data.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return _data.TryGetValue(key, out value);
        }
    }

    private void SampleMethod_ComplexType(Dictionary<string, Person> parameter = null) { }
    private void SampleMethod_SimpleType(Dictionary<string, int> parameter = null) { }
}
