// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class OutputFormatterDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotOutputFormatter()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.OutputFormatter'.";

            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new OutputFormatterDescriptor(type), "outputFormatterType", expected);
        }
    }
}