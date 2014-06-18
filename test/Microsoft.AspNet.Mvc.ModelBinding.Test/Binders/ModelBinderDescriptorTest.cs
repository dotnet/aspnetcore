// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBinderDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotIModelBinder()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinder'.";
            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new ModelBinderDescriptor(type), "modelBinderType", expected);
        }
    }
}