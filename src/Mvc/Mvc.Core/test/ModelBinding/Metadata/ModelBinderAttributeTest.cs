// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelBinderAttributeTest
{
    [Fact]
    public void NoBinderType_NoBindingSource()
    {
        // Arrange
        var attribute = new ModelBinderAttribute();

        // Act
        var source = attribute.BindingSource;

        // Assert
        Assert.Null(source);
    }

    [Fact]
    public void BinderType_DefaultCustomBindingSource()
    {
        // Arrange
        var attribute = new ModelBinderAttribute
        {
            BinderType = typeof(ByteArrayModelBinder),
        };

        // Act
        var source = attribute.BindingSource;

        // Assert
        Assert.Same(BindingSource.Custom, source);
    }

    [Fact]
    public void BinderTypePassedToConstructor_DefaultCustomBindingSource()
    {
        // Arrange
        var attribute = new ModelBinderAttribute(typeof(ByteArrayModelBinder));

        // Act
        var source = attribute.BindingSource;

        // Assert
        Assert.Same(BindingSource.Custom, source);
    }

    [Fact]
    public void BinderType_SettingBindingSource_OverridesDefaultCustomBindingSource()
    {
        // Arrange
        var attribute = new FromQueryModelBinderAttribute
        {
            BinderType = typeof(ByteArrayModelBinder)
        };

        // Act
        var source = attribute.BindingSource;

        // Assert
        Assert.Equal(BindingSource.Query, source);
    }

    private class FromQueryModelBinderAttribute : ModelBinderAttribute
    {
        public override BindingSource BindingSource => BindingSource.Query;
    }
}
