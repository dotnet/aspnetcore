// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

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
}
