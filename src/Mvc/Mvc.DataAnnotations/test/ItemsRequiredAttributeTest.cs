// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class ItemsRequiredAttributeTest
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(new[] { "one", "two" }, true)]
    [InlineData(new[] { "one", "two", null }, false)]
    public void ItemsRequiredAttribute_CanValidateArrays(object value, bool isValid)
    {
        var attribute = new ItemsRequiredAttribute();
        Assert.Equal(isValid, attribute.IsValid(value));
    }

    [Fact]
    public void ItemsRequiredAttribute_CanValidateLists()
    {
        List<string> listWithNullElement = new() { "one", "two", null };
        List<string> listWithoutNullElement = new() { "one", "two" };

        var attribute = new ItemsRequiredAttribute();
        Assert.False(attribute.IsValid(listWithNullElement));
        Assert.True(attribute.IsValid(listWithoutNullElement));
    }
}
