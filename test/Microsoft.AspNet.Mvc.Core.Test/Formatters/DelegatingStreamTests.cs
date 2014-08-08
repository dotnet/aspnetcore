// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System.IO;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class DelegatingStreamTests
    {
        [Fact]
        public void InnerStreamIsOpenOnClose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var delegatingStream = new DelegatingStream(innerStream);

            // Act
            delegatingStream.Close();

            // Assert
            Assert.True(innerStream.CanRead);
        }

        [Fact]
        public void InnerStreamIsOpenOnDispose()
        {
            // Arrange
            var innerStream = new MemoryStream();
            var delegatingStream = new DelegatingStream(innerStream);

            // Act
            delegatingStream.Dispose();

            // Assert
            Assert.True(innerStream.CanRead);
        }
    }
}
#endif