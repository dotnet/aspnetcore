// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class InputFormatterDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotInputFormatter()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.ModelBinding.IInputFormatter'.";

            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new InputFormatterDescriptor(type), "type", expected);
        }
    }
}