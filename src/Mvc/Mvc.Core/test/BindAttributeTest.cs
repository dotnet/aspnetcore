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
    [InlineData("TestProperty", false)]
    [InlineData("foo", true)]
    [InlineData("bar", true)]
    public void BindAttribute_Include(string property, bool isIncluded)
    {
        // Arrange
        var bind = new BindAttribute(new string[] { "UserName", "FirstName", "LastName, MiddleName,  ,foo,bar " });

        var context = new DefaultModelBindingContext();

        var propertyInfo = typeof(TestContainer).GetProperty(property);
        var identity = ModelMetadataIdentity.ForProperty(propertyInfo!, typeof(int), typeof(TestContainer));
        context.ModelMetadata = new Mock<ModelMetadata>(identity).Object;

        // Act
        var propertyFilter = bind.PropertyFilter;

        // Assert
        Assert.Equal(isIncluded, propertyFilter(context.ModelMetadata));
    }

    private sealed class TestContainer
    {
        public int UserName { get; set; }
        public int Username { get; set; }
        public int Password { get; set; }
        public int LastName { get; set; }
        public int MiddleName { get; set; }
        public int foo { get; set; }
        public int bar { get; set; }
        public int TestProperty { get; set; }
    }
}
