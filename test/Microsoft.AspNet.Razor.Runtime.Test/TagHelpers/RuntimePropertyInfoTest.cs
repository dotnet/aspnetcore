// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class RuntimePropertyInfoTest
    {
        [Fact]
        public void PropertyInfo_ReturnsMetadataOfAdaptingProperty()
        {
            // Arrange
            var property = GetPropertyInfo(nameof(TestType.Property));
            var runtimePropertyInfo = new RuntimePropertyInfo(property);

            // Act
            var actual = runtimePropertyInfo.Property;

            // Assert
            Assert.Same(property, actual);
            var runtimeTypeInfo = Assert.IsType<RuntimeTypeInfo>(runtimePropertyInfo.PropertyType);
            Assert.Same(property.PropertyType, runtimeTypeInfo.TypeInfo);
        }

        [Theory]
        [InlineData(nameof(TestType.Property))]
        [InlineData(nameof(TestType.PrivateSetter))]
        [InlineData(nameof(TestType.PropertyWithoutSetter))]
        public void HasPublicGetter_ReturnsTrueIfGetterExistsAndIsPublic(string propertyName)
        {
            // Arrange
            var property = GetPropertyInfo(propertyName);
            var runtimePropertyInfo = new RuntimePropertyInfo(property);

            // Act
            var result = runtimePropertyInfo.HasPublicGetter;

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(TestType.PrivateGetter))]
        [InlineData(nameof(TestType.PropertyWithoutGetter))]
        [InlineData("ProtectedProperty")]
        public void HasPublicGetter_ReturnsFalseIfGetterDoesNotExistOrIsNonPublic(string propertyName)
        {
            // Arrange
            var property = GetPropertyInfo(propertyName);
            var runtimePropertyInfo = new RuntimePropertyInfo(property);

            // Act
            var result = runtimePropertyInfo.HasPublicGetter;

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(nameof(TestType.Property))]
        [InlineData(nameof(TestType.PrivateGetter))]
        [InlineData(nameof(TestType.PropertyWithoutGetter))]
        public void HasPublicSetter_ReturnsTrueIfSetterExistsAndIsPublic(string propertyName)
        {
            // Arrange
            var property = GetPropertyInfo(propertyName);
            var runtimePropertyInfo = new RuntimePropertyInfo(property);

            // Act
            var result = runtimePropertyInfo.HasPublicSetter;

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(TestType.PrivateSetter))]
        [InlineData(nameof(TestType.PropertyWithoutSetter))]
        [InlineData("ProtectedProperty")]
        public void HasPublicSetter_ReturnsFalseIfGetterDoesNotExistOrIsNonPublic(string propertyName)
        {
            // Arrange
            var property = GetPropertyInfo(propertyName);
            var runtimePropertyInfo = new RuntimePropertyInfo(property);

            // Act
            var result = runtimePropertyInfo.HasPublicSetter;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAttributes_ReturnsCustomAttributesOfSpecifiedType()
        {
            // Arrange
            var property = GetPropertyInfo(nameof(TestType.PropertyWithAttributes));
            var runtimeProperty = new RuntimePropertyInfo(property);

            // Act
            var attributes = property.GetCustomAttributes<HtmlAttributeNameAttribute>();

            // Assert
            var htmlAttributeName = Assert.Single(attributes);
            Assert.Equal("somename", htmlAttributeName.Name);
        }

        [Fact]
        public void GetAttributes_DoesNotInheritAttributes()
        {
            // Arrange
            var property = GetPropertyInfo(nameof(TestType.PropertyWithAttributes));
            var runtimeProperty = new RuntimePropertyInfo(property);

            // Act
            var attributes = property.GetCustomAttributes<HtmlAttributeNotBoundAttribute>();

            // Assert
            Assert.Empty(attributes);
        }

        private static PropertyInfo GetPropertyInfo(string propertyName)
        {
            return typeof(TestType).GetRuntimeProperties()
                .FirstOrDefault(p => p.Name == propertyName);
        }

        public class BaseType
        {
            [HtmlAttributeNotBound]
            public virtual string PropertyWithAttributes { get; }
        }

        public class TestType : BaseType
        {
            public string Property { get; set; }

            public int PrivateSetter { get; private set; }

            public object PrivateGetter { private get; set; }

            protected DateTimeOffset ProtectedProperty { get; set; }

            public string PropertyWithoutGetter
            {
                set { }
            }

            public int PropertyWithoutSetter => 0;

            [HtmlAttributeName("somename")]
            public override string PropertyWithAttributes { get; }
        }
    }
}
