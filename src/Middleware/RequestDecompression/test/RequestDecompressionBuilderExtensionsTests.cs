// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.RequestDecompression.Tests;

public class RequestDecompressionBuilderExtensionsTests
{
    [Fact]
    public void UseRequestDecompression_NullApplicationBuilder_Throws()
    {
        // Arrange
        IApplicationBuilder builder = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            builder.UseRequestDecompression();
        });
    }
}
