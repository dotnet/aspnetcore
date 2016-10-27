// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public class IRBuilderTest
    {
        [Fact]
        public void Document_CreatesDocumentNode()
        {
            // Arrange & Act 
            var builder = IRBuilder.Document();

            // Assert
            Assert.IsType<IRDocument>(builder.Current);
        }
    }
}
