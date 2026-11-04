// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class CancellationTokenModelBinderIntegrationTest
{
    private class Person
    {
        public CancellationToken Token { get; set; }
    }

    [Fact]
    public async Task BindProperty_WithData_WithPrefix_GetsBound()
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

            ParameterType = typeof(Person)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.False(boundPerson.Token.IsCancellationRequested);
        testContext.HttpContext.Abort();
        Assert.True(boundPerson.Token.IsCancellationRequested);

        // ModelState
        Assert.True(modelState.IsValid);

        Assert.Empty(modelState.Keys);
    }

    [Fact]
    public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(Person)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.False(boundPerson.Token.IsCancellationRequested);
        testContext.HttpContext.Abort();
        Assert.True(boundPerson.Token.IsCancellationRequested);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    [Fact]
    public async Task BindParameter_WithData_GetsBound()
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

            ParameterType = typeof(CancellationToken)
        };

        var testContext = ModelBindingTestHelper.GetTestContext();
        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var token = Assert.IsType<CancellationToken>(modelBindingResult.Model);
        Assert.False(token.IsCancellationRequested);
        testContext.HttpContext.Abort();
        Assert.True(token.IsCancellationRequested);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }
}
