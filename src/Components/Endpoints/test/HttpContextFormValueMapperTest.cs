// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Components.Forms.Mapping;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class HttpContextFormValueMapperTest
{
    // Cases with no scope in effect
    [InlineData(true, "some-form", "", null)]           // No form restriction
    [InlineData(true, "some-form", "", "some-form")]    // Matching form
    [InlineData(false, "some-form", "", "other-form")]  // Mismatching form
    [InlineData(false, "some-form", "x", "some-form")]  // Mismatching scope

    // Cases with scope in effect
    [InlineData(true, "[scope-name]some-form", "scope-name", null)]             // Matching scope, no form restriction
    [InlineData(true, "[scope-name]some-form", "scope-name", "some-form")]      // Matching scope, matching form
    [InlineData(false, "[scope-name]some-form", "scope-name", "other-form")]    // Matching scope, mismatching form
    [InlineData(false, "[scope-name]some-form", "other-scope", null)]           // Mismatching scope, no form restriction
    [InlineData(false, "[scope-name]some-form", "other-scope", "some-form")]    // Mismatching scope, matching form
    [InlineData(false, "[scope-name]some-form", "other-scope", "other-form")]   // Mismatching scope, mismatching form
    [InlineData(false, "[scope]", "longerstring", null)] // Show we don't try to read too many characters from the scope section

    // Invalid incoming form handler name
    [InlineData(false, "[something", "something", null)] // Unterminated scope name shouldn't match on scope
    [InlineData(false, "[something", "", "something")] // Unterminated scope name shouldn't match on form
    [InlineData(false, "something]", "something", null)]
    [InlineData(false, "something]", "", "something")]
    [InlineData(false, "[a][b]", "b", null)] // Scope name is only counted as the first bracketed item
    [Theory]
    public void CanMap_MatchesOnScopeAndFormName(bool expectedResult, string incomingFormName, string scopeName, string formNameOrNull)
    {
        var formData = new HttpContextFormDataProvider();
        formData.SetFormData(incomingFormName, new Dictionary<string, StringValues>(), new FormFileCollection());

        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));

        var canMap = mapper.CanMap(typeof(string), scopeName, formNameOrNull);
        Assert.Equal(expectedResult, canMap);
    }

    [Fact]
    public void CanMap_SimpleRecursiveModel_ReturnsTrue()
    {
        // Test that CanMap works correctly for recursive types (GitHub issue #61341)
        var formData = new HttpContextFormDataProvider();
        formData.SetFormData("test-form", new Dictionary<string, StringValues>
        {
            ["Name"] = "Test Name"
        }, new FormFileCollection());

        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));

        var canMap = mapper.CanMap(typeof(MyModel), "", null);
        Assert.True(canMap);
    }

    [Fact]
    public void Map_SetsNullResult_WhenCanMapReturnsFalse()
    {
        // This test verifies the fix for GitHub issue #61341
        // The Map method should return early when CanMap returns false
        var formData = new HttpContextFormDataProvider();
        // Don't set any form data so CanMap will return false due to no incoming handler name
        
        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));
        var context = new FormValueMappingContext("", null, typeof(MyModel), "Model");

        // Act
        mapper.Map(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void Map_DoesNotDeserializeWhenCanMapReturnsFalse()
    {
        // This test demonstrates the original bug and verifies the fix
        // Before the fix, Map would call deserializer even when CanMap returned false
        var formData = new HttpContextFormDataProvider();
        // Set form data but no incoming handler name, so CanMap returns false
        formData.SetFormData("", new Dictionary<string, StringValues>
        {
            ["Name"] = "Test"
        }, new FormFileCollection());
        
        var mapper = new HttpContextFormValueMapper(formData, Options.Create<RazorComponentsServiceOptions>(new()));
        var context = new FormValueMappingContext("mismatched-scope", null, typeof(MyModel), "Model");

        // Act
        mapper.Map(context);

        // Assert - The result should be null because CanMap returns false
        // Before the fix, this might have been a MyModel instance due to deserializer running
        Assert.Null(context.Result);
    }

    [Fact]
    public void SupplyParameterFromForm_WithRecursiveModel_ExactGitHubIssueScenario()
    {
        // Arrange - Reproduce the EXACT scenario from GitHub issue #61341
        var formData = new Dictionary<string, StringValues>()
        {
            ["Name"] = "Test Name"
            // Note: No data for Parent property, but it should still bind the Name
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        
        // Set up form data provider to simulate the exact scenario
        var formDataProvider = new HttpContextFormDataProvider();
        formDataProvider.SetFormData("RequestForm", formData, new FormFileCollection());
        
        // Create the mapper (this will use our fix)
        var options = Options.Create(new RazorComponentsServiceOptions());
        var mapper = new HttpContextFormValueMapper(formDataProvider, options);

        // Act - Try to map the recursive model type (the exact case that was failing)
        var context = new FormValueMappingContext("", "RequestForm", typeof(MyModel), "Model");
        mapper.Map(context);

        // Assert - The model should be created successfully, not null
        // Before the fix, this would be null due to the missing return statement
        Assert.NotNull(context.Result);
        var result = Assert.IsType<MyModel>(context.Result);
        Assert.Equal("Test Name", result.Name);
        Assert.Null(result.Parent); // Parent should be null since no data was provided
    }

    [Fact]
    public void CanMap_WithRecursiveModel_ShouldReturnTrue()
    {
        // Arrange - Test the CanMap method specifically for recursive types
        var formDataProvider = new HttpContextFormDataProvider();
        formDataProvider.SetFormData("RequestForm", new Dictionary<string, StringValues>(), new FormFileCollection());
        
        var options = Options.Create(new RazorComponentsServiceOptions());
        var mapper = new HttpContextFormValueMapper(formDataProvider, options);

        // Act
        var canMap = mapper.CanMap(typeof(MyModel), "", "RequestForm");

        // Assert - Should be able to map recursive model types
        Assert.True(canMap, "Should be able to map recursive model types");
    }
}

internal class MyModel
{
    public string Name { get; set; }
    public MyModel Parent { get; set; }
}
