// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultAllowedChildTagDescriptorBuilderTest
    {
        [Fact]
        public void Build_DisplayNameIsName()
        {
            // Arrange
            var builder = new DefaultAllowedChildTagDescriptorBuilder(null);
            builder.Name = "foo";

            // Act
            var descriptor = builder.Build();

            // Assert
            Assert.Equal("foo", descriptor.DisplayName);
        }
    }
}
