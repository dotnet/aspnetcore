// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
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
            var attribute = new ModelBinderAttribute
            {
                BinderType = typeof(ByteArrayModelBinder),
            };

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Same(BindingSource.Custom, source);
        }

        [Fact]
        public void BinderTypePassedToConstructor_DefaultCustomBindingSource()
        {
            // Arrange
            var attribute = new ModelBinderAttribute(typeof(ByteArrayModelBinder));

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Same(BindingSource.Custom, source);
        }

        [Fact]
        public void BinderType_SettingBindingSource_OverridesDefaultCustomBindingSource()
        {
            // Arrange
            var attribute = new FromQueryModelBinderAttribute
            {
                BinderType = typeof(ByteArrayModelBinder)
            };

            // Act
            var source = attribute.BindingSource;

            // Assert
            Assert.Equal(BindingSource.Query, source);
        }

        private class FromQueryModelBinderAttribute : ModelBinderAttribute
        {
            public override BindingSource BindingSource => BindingSource.Query;
        }
    }
}
