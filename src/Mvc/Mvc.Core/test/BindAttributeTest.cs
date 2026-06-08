// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class BindAttributeTest
{
    [Theory]
    [InlineData("UserName", true)]
    [InlineData("Username", false)]
    [InlineData("Password", false)]
    [InlineData("LastName", true)]
    [InlineData("MiddleName", true)]
    [InlineData(" ", false)]
    [InlineData("foo", true)]
    [InlineData("bar", true)]
    public void BindAttribute_Include(string property, bool isIncluded)
    {
        // Arrange
        var bind = new BindAttribute(new string[] { "UserName", "FirstName", "LastName, MiddleName,  ,foo,bar " });

        var context = new DefaultModelBindingContext();

        var identity = CreatePropertyIdentity(property);
        context.ModelMetadata = new Mock<ModelMetadata>(identity).Object;

        // Act
        var propertyFilter = bind.PropertyFilter;

        // Assert
        Assert.Equal(isIncluded, propertyFilter(context.ModelMetadata));
    }

    private static ModelMetadataIdentity CreatePropertyIdentity(string propertyName)
    {
        var constructor = typeof(ModelMetadataIdentity).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            [typeof(Type), typeof(string), typeof(Type), typeof(object), typeof(ConstructorInfo)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (ModelMetadataIdentity)constructor.Invoke([typeof(int), propertyName, typeof(string), null, null]);
    }
}
