// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.AspNet.Http.Core.Tests
{
    public class BufferingHelperTests
    {
        [Fact]
        public void GetTempDirectory_Returns_Valid_Location()
        {
            var tempDirectory = BufferingHelper.TempDirectory;
            Assert.NotNull(tempDirectory);
            Assert.True(Directory.Exists(tempDirectory));
        }
    }
}