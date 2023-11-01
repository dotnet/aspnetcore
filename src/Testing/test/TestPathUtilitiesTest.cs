// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class TestPathUtilitiesTest
{
    // Entire test pending removal - see https://github.com/dotnet/extensions/issues/1697
#pragma warning disable 0618

    [Fact]
    public void GetSolutionRootDirectory_Throws_IfNotFound()
    {
        var exception = Assert.Throws<Exception>(() => TestPathUtilities.GetSolutionRootDirectory("NotTesting"));
        Assert.Equal($"Solution file NotTesting.slnf could not be found in {AppContext.BaseDirectory} or its parent directories.", exception.Message);
    }
#pragma warning restore 0618
}
