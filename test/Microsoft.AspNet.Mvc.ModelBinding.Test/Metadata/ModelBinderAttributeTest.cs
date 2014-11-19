// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBinderAttributeTest
    {
        [Fact]
        public void InvalidBinderType_Throws()
        {
            // Arrange
            var attribute = new ModelBinderAttribute();

            var expected =
                "The type 'System.String' must implement either " +
                "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinder' or " +
                "'Microsoft.AspNet.Mvc.ModelBinding.IModelBinderProvider' to be used as a model binder.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { attribute.BinderType = typeof(string); });

            // Assert
            Assert.Equal(expected, ex.Message);
        }
    }
}