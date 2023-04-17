// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;

namespace Microsoft.AspNetCore.Components;

public class ModelBindingContextTest
{
    [Fact]
    public void Throws_IfNameAndBindingContextId_AreProvided()
    {
        Assert.Throws<InvalidOperationException>(() => new ModelBindingContext("name", "id"));
    }

    [Fact]
    public void Throws_IfNoNameOrBindingContextId_AreProvided()
    {
        Assert.Throws<InvalidOperationException>(() => new ModelBindingContext("name", "id"));
    }

    [Fact]
    public void Name_UsedAsBindingContextId_WhenProvided()
    {
        var context = new ModelBindingContext("navigation");
        Assert.Equal("navigation", context.Name);
        Assert.Equal("navigation", context.BindingContextId);
    }

    [Fact]
    public void CanProvide_BindingContextId_ForDefaultName()
    {
        var context = new ModelBindingContext("", "binding-context");
        Assert.Equal("", context.Name);
        Assert.Equal("binding-context", context.BindingContextId);
    }
}
