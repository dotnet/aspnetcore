// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class ModelBindingContextTest
{
    [Fact]
    public void CanCreate_BindingContext_WithDefaultName()
    {
        var context = new ModelBindingContext("", "");
        Assert.Equal("", context.Name);
        Assert.Equal("", context.BindingContextId);
    }

    [Fact]
    public void CanCreate_BindingContext_WithName()
    {
        var context = new ModelBindingContext("name", "path?handler=name");
        Assert.Equal("name", context.Name);
        Assert.Equal("path?handler=name", context.BindingContextId);
    }

    [Fact]
    public void Throws_WhenNameIsProvided_AndNoBindingContextId()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ModelBindingContext("name", ""));
        Assert.Equal("A root binding context needs to provide a name and explicit binding context id or none.", exception.Message);
    }

    [Fact]
    public void Throws_WhenBindingContextId_IsProvidedForDefaultName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ModelBindingContext("", "context"));
        Assert.Equal("A root binding context needs to provide a name and explicit binding context id or none.", exception.Message);
    }
}
