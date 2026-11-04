// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging.Tests;

public class HttpLoggingAttributeTests
{
    [Fact]
    public void ThrowsForInvalidOptions()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new HttpLoggingAttribute(HttpLoggingFields.None) { RequestBodyLogLimit = -1 });
        Assert.Equal(nameof(HttpLoggingAttribute.RequestBodyLogLimit), ex.ParamName);

        ex = Assert.Throws<ArgumentOutOfRangeException>(() => new HttpLoggingAttribute(HttpLoggingFields.None) { ResponseBodyLogLimit = -1 });
        Assert.Equal(nameof(HttpLoggingAttribute.ResponseBodyLogLimit), ex.ParamName);
    }

    [Fact]
    public void ThrowsWhenAccessingFieldsThatAreNotSet()
    {
        var attribute = new HttpLoggingAttribute(HttpLoggingFields.None);

        Assert.Throws<InvalidOperationException>(() => attribute.RequestBodyLogLimit);
        Assert.Throws<InvalidOperationException>(() => attribute.ResponseBodyLogLimit);
    }
}
