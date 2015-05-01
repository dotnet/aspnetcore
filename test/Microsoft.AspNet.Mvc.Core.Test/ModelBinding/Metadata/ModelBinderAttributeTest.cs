// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelBinderAttributeTest
    {
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