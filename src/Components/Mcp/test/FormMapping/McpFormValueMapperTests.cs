// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Mcp.FormMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpFormValueMapperTests
{
    [Theory]
    [InlineData(true, "form", "", null)]
    [InlineData(true, "form", "", "form")]
    [InlineData(false, "form", "", "other")]
    [InlineData(false, "form", "scope", "form")]
    [InlineData(true, "[scope]form", "scope", null)]
    [InlineData(true, "[scope]form", "scope", "form")]
    [InlineData(false, "[scope]form", "scope", "other")]
    [InlineData(false, "[scope]form", "other", null)]
    [InlineData(false, "[scope]form", "other", "form")]
    [InlineData(false, "[scope]", "longer", null)]
    [InlineData(false, "[scope", "scope", null)]
    [InlineData(false, "[scope", "", "scope")]
    [InlineData(false, "scope]", "scope", null)]
    [InlineData(false, "scope]", "", "scope")]
    [InlineData(false, "[a][b]", "b", null)]
    public void CanMap_MatchesScopeAndForm(bool expected, string handler, string scope, string? form)
    {
        var data = new McpFormDataProvider();
        data.Initialize(handler, new Dictionary<string, StringValues>(), new FormFileCollection());
        var mapper = new McpFormValueMapper(data, new FormDataMapperOptions());

        Assert.Equal(expected, mapper.CanMap(typeof(string), scope, form));
    }

    [Fact]
    public void CanMap_WithoutSubmission_DoesNotSupplyAValue()
    {
        var mapper = new McpFormValueMapper(new McpFormDataProvider(), new FormDataMapperOptions());

        Assert.False(mapper.CanMap(typeof(string), "", null));
    }

    [Fact]
    public void Initialize_RejectsReinitializationWithinAnOperation()
    {
        var data = new McpFormDataProvider();
        var originalEntries = new Dictionary<string, StringValues> { ["Input"] = "original" };
        data.Initialize("original", originalEntries, new FormFileCollection());

        Assert.Throws<InvalidOperationException>(
            () => data.Initialize("replacement", new Dictionary<string, StringValues>(), new FormFileCollection()));
        Assert.Equal("original", data.HandlerName);
        Assert.Same(originalEntries, data.Entries);
    }
}
