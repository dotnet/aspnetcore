// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class NullableReferenceTypeIntegrationTest
{
#nullable enable
    private class Person1
    {
        public string FirstName { get; set; } = default!;
    }
#nullable restore

    [Fact]
    public async Task BindProperty_WithNonNullableReferenceType_NoData_ValidationError()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person1)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person1>(modelBindingResult.Model);
        Assert.Null(boundPerson.FirstName);

        // ModelState
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("FirstName", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);

                // Not validating framework error message.
                Assert.Single(kvp.Value.Errors);
            });
    }

#nullable enable
    private class Person2
    {
        public string? FirstName { get; set; }
    }
#nullable restore

    [Fact]
    public async Task BindProperty_WithNullableReferenceType_NoData_NoValidationError()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person2)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person2>(modelBindingResult.Model);
        Assert.Null(boundPerson.FirstName);

        // ModelState
        Assert.True(modelState.IsValid);
    }

#nullable enable
    private class Person3
    {
        [Required(ErrorMessage = "Test")]
        public string FirstName { get; set; } = default!;
    }
#nullable restore

    [Fact]
    public async Task BindProperty_WithNonNullableReferenceType_NoData_ValidationError_CustomMessage()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person3)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person3>(modelBindingResult.Model);
        Assert.Null(boundPerson.FirstName);

        // ModelState
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("FirstName", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);

                var error = Assert.Single(kvp.Value.Errors);
                Assert.Equal("Test", error.ErrorMessage);
            });
    }

#nullable enable
    private void NonNullableParameter(string param1)
    {
    }
#nullable restore

    [Fact]
    public async Task BindParameter_WithNonNullableReferenceType_NoData_ValidationError()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "param1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(string)
        };

        var method = GetType().GetMethod(nameof(NonNullableParameter), BindingFlags.NonPublic | BindingFlags.Instance);
        var parameterInfo = method.GetParameters().Single();
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelMetadata = modelMetadataProvider.GetMetadataForParameter(parameterInfo);

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(
            parameter,
            testContext,
            modelMetadataProvider,
            modelMetadata);

        // Assert

        // ModelBindingResult
        Assert.False(modelBindingResult.IsModelSet);

        // Model
        Assert.Null(modelBindingResult.Model);

        // ModelState
        Assert.False(modelState.IsValid);
        Assert.Collection(
            modelState.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("param1", kvp.Key);
                Assert.Equal(ModelValidationState.Invalid, kvp.Value.ValidationState);

                // Not validating framework error message.
                Assert.Single(kvp.Value.Errors);
            });
    }
}
