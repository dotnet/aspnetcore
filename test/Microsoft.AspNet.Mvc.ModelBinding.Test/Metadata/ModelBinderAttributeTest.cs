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
                $"The type 'System.String' must implement '{typeof(IModelBinder).FullName}' " +
                "to be used as a model binder.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { attribute.BinderType = typeof(string); });

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void NoBinderType_NoBindingSource()
        {
            // Arrange
            var attribute = new ModelBinderAttribute();

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Null(source);
        }

        [Fact]
        public void BinderType_DefaultCustomBindingSource()
        {
            // Arrange
            var attribute = new ModelBinderAttribute();
            attribute.BinderType = typeof(ByteArrayModelBinder);

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Equal(BindingSource.Custom, source);
        }

        [Fact]
        public void BinderType_SettingBindingSource_OverridesDefaultCustomBindingSource()
        {
            // Arrange
            var attribute = new ModelBinderAttribute();
            attribute.BindingSource = BindingSource.Query;
            attribute.BinderType = typeof(ByteArrayModelBinder);

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Equal(BindingSource.Query, source);
        }
    }
}