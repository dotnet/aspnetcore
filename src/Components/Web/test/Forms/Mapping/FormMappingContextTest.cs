// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components;

public class FormMappingContextTest
{
    [Fact]
    public void CanCreate_MappingContext_WithDefaultName()
    {
        var context = new FormMappingContext("");
        Assert.Equal("", context.MappingScopeName);
    }

    [Fact]
    public void CanCreate_MappingContext_WithName()
    {
        var context = new FormMappingContext("name");
        Assert.Equal("name", context.MappingScopeName);
    }
}
