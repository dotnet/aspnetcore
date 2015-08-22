// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadataTest
    {
        // IsComplexType
        private struct IsComplexTypeModel
        {
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(Nullable<int>))]
        [InlineData(typeof(int))]
        public void IsComplexType_ReturnsFalseForSimpleTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new TestModelMetadata(type);

            // Assert
            Assert.False(modelMetadata.IsComplexType);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(IsComplexTypeModel))]
        [InlineData(typeof(Nullable<IsComplexTypeModel>))]
        public void IsComplexType_ReturnsTrueForComplexTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new TestModelMetadata(type);

            // Assert
            Assert.True(modelMetadata.IsComplexType);
        }

        // IsCollectionType
        private class NonCollectionType
        {
        }

        private class DerivedList : List<int>
        {
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(NonCollectionType))]
        [InlineData(typeof(string))]
        public void IsCollectionType_ReturnsFalseForNonCollectionTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new TestModelMetadata(type);

            // Assert
            Assert.False(modelMetadata.IsCollectionType);
        }

        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(DerivedList))]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(Dictionary<object, object>))]
        public void IsCollectionType_ReturnsTrueForCollectionTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new TestModelMetadata(type);

            // Assert
            Assert.True(modelMetadata.IsCollectionType);
        }

        // IsNullableValueType

        [Theory]
        [InlineData(typeof(string), false)]
        [InlineData(typeof(IDisposable), false)]
        [InlineData(typeof(Nullable<int>), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(DerivedList), false)]
        [InlineData(typeof(IsComplexTypeModel), false)]
        [InlineData(typeof(Nullable<IsComplexTypeModel>), true)]
        public void IsNullableValueType_ReturnsExpectedValue(Type modelType, bool expected)
        {
            // Arrange
            var modelMetadata = new TestModelMetadata(modelType);

            // Act & Assert
            Assert.Equal(expected, modelMetadata.IsNullableValueType);
        }

        // IsReferenceOrNullableType

        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(IDisposable), true)]
        [InlineData(typeof(Nullable<int>), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(DerivedList), true)]
        [InlineData(typeof(IsComplexTypeModel), false)]
        [InlineData(typeof(Nullable<IsComplexTypeModel>), true)]
        public void IsReferenceOrNullableType_ReturnsExpectedValue(Type modelType, bool expected)
        {
            // Arrange
            var modelMetadata = new TestModelMetadata(modelType);

            // Act & Assert
            Assert.Equal(expected, modelMetadata.IsReferenceOrNullableType);
        }

        // UnderlyingOrModelType

        [Theory]
        [InlineData(typeof(string), typeof(string))]
        [InlineData(typeof(IDisposable), typeof(IDisposable))]
        [InlineData(typeof(Nullable<int>), typeof(int))]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(DerivedList), typeof(DerivedList))]
        [InlineData(typeof(IsComplexTypeModel), typeof(IsComplexTypeModel))]
        [InlineData(typeof(Nullable<IsComplexTypeModel>), typeof(IsComplexTypeModel))]
        public void UnderlyingOrModelType_ReturnsExpectedValue(Type modelType, Type expected)
        {
            // Arrange
            var modelMetadata = new TestModelMetadata(modelType);

            // Act & Assert
            Assert.Equal(expected, modelMetadata.UnderlyingOrModelType);
        }

        // GetDisplayName()

        [Fact]
        public void GetDisplayName_ReturnsDisplayName_IfSet()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new TestModelMetadata(typeof(int), "Length", typeof(string));
            metadata.SetDisplayName("displayName");

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("displayName", result);
        }

        [Fact]
        public void GetDisplayName_ReturnsPropertyName_WhenSetAndDisplayNameIsNull()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new TestModelMetadata(typeof(int), "Length", typeof(string));

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("Length", result);
        }

        [Fact]
        public void GetDisplayName_ReturnsTypeName_WhenPropertyNameAndDisplayNameAreNull()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new TestModelMetadata(typeof(string));

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("String", result);
        }

        private class TestModelMetadata : ModelMetadata
        {
            private string _displayName;

            public TestModelMetadata(Type modelType)
                : base(ModelMetadataIdentity.ForType(modelType))
            {
            }

            public TestModelMetadata(Type modelType, string propertyName, Type containerType)
                : base(ModelMetadataIdentity.ForProperty(modelType, propertyName, containerType))
            {
            }

            public override IReadOnlyDictionary<object, object> AdditionalValues
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string BinderModelName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Type BinderType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override BindingSource BindingSource
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool ConvertEmptyStringToNull
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string DataTypeName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string Description
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string DisplayFormatString
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string DisplayName
            {
                get
                {
                    return _displayName;
                }
            }

            public void SetDisplayName(string displayName)
            {
                _displayName = displayName;
            }

            public override string EditFormatString
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override ModelMetadata ElementMetadata
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IEnumerable<KeyValuePair<string, string>> EnumDisplayNamesAndValues
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IReadOnlyDictionary<string, string> EnumNamesAndValues
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool HasNonDefaultEditFormat
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool HideSurroundingHtml
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool HtmlEncode
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsBindingAllowed
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsBindingRequired
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsEnum
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsFlagsEnum
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsRequired
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string NullDisplayText
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override int Order
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override ModelPropertyCollection Properties
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IPropertyBindingPredicateProvider PropertyBindingPredicateProvider
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool ShowForDisplay
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool ShowForEdit
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string SimpleDisplayProperty
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string TemplateHint
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override IReadOnlyList<object> ValidatorMetadata
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Func<object, object> PropertyGetter
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Action<object, object> PropertySetter
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
