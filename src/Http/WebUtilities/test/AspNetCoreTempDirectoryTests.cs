// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Internal
{
    public class AspNetCoreTempDirectoryTests
    {
        [Fact]
        public void GetTempDirectory_Returns_Valid_Location()
        {
            var tempDirectory = AspNetCoreTempDirectory.TempDirectory;
            Assert.NotNull(tempDirectory);
            Assert.True(Directory.Exists(tempDirectory));
        }
    }
}
