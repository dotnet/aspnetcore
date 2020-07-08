// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
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
}
