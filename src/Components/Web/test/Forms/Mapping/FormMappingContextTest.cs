// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components;

public class FormMappingContextTest
{
    [Fact]
    public void CanCreate_MappingContext_WithDefaultName()
    {
        var context = new FormMappingContext("", "");
        Assert.Equal("", context.Name);
        Assert.Equal("", context.MappingContextId);
    }

    [Fact]
    public void CanCreate_MappingContext_WithName()
    {
        var context = new FormMappingContext("name", "path?handler=name");
        Assert.Equal("name", context.Name);
        Assert.Equal("path?handler=name", context.MappingContextId);
    }

    [Fact]
    public void Throws_WhenNameIsProvided_AndNoMappingContextId()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new FormMappingContext("name", ""));
        Assert.Equal("A root mapping context needs to provide a name and explicit mapping context id or none.", exception.Message);
    }

    [Fact]
    public void Throws_WhenMappingContextId_IsProvidedForDefaultName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new FormMappingContext("", "context"));
        Assert.Equal("A root mapping context needs to provide a name and explicit mapping context id or none.", exception.Message);
    }
}
