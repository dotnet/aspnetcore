// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class ByteArrayModelBinderIntegrationTest
{
    private class Person
    {
        public byte[] Token { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BindProperty_WithData_GetsBound(bool fallBackScenario)
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person)
        };

        var prefix = fallBackScenario ? string.Empty : "Parameter1";
        var queryStringKey = fallBackScenario ? "Token" : prefix + "." + "Token";

        // any valid base64 string
        var expectedValue = new byte[] { 12, 13 };
        var value = Convert.ToBase64String(expectedValue);
        var testContext = ModelBindingTestHelper.GetTestContext(
            request =>
            {
                request.QueryString = QueryString.Create(queryStringKey, value);
            });
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.NotNull(boundPerson.Token);
        Assert.Equal(expectedValue, boundPerson.Token);

        // ModelState
        Assert.True(modelState.IsValid);

        var entry = Assert.Single(modelState);
        Assert.Equal(queryStringKey, entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
        Assert.Equal(value, entry.Value.AttemptedValue);
        Assert.Equal(value, entry.Value.RawValue);
    }

    [Fact]
    public async Task BindParameter_NoData_DoesNotGetBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo()
            {
                BinderModelName = "CustomParameter",
            },

            ParameterType = typeof(byte[])
        };

        // No data is passed.
        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.False(modelBindingResult.IsModelSet);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState.Keys);
    }

    [Fact]
    public async Task BindParameter_WithData_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo
            {
                BinderModelName = "CustomParameter",
            },
            ParameterType = typeof(byte[])
        };

        // any valid base64 string
        var value = "four";
        var expectedValue = Convert.FromBase64String(value);
        var testContext = ModelBindingTestHelper.GetTestContext(
            request =>
            {
                request.QueryString = QueryString.Create("CustomParameter", value);
            });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);
        var model = Assert.IsType<byte[]>(modelBindingResult.Model);

        // Model
        Assert.Equal(expectedValue, model);

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("CustomParameter", entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(value, entry.Value.AttemptedValue);
        Assert.Equal(value, entry.Value.RawValue);
    }
}
