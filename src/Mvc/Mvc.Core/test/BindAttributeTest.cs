// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;

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

#pragma warning disable CS0618 // Type or member is obsolete
        var identity = ModelMetadataIdentity.ForProperty(typeof(int), property, typeof(string));
#pragma warning restore CS0618 // Type or member is obsolete
        context.ModelMetadata = new Mock<ModelMetadata>(identity).Object;

        // Act
        var propertyFilter = bind.PropertyFilter;

        // Assert
        Assert.Equal(isIncluded, propertyFilter(context.ModelMetadata));
    }
}
