// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelMetadataTest
{
    // IsComplexType
    private readonly struct IsComplexTypeModel
    {
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(Nullable<int>))]
    [InlineData(typeof(int))]
    public void IsComplexType_ReturnsFalseForSimpleTypes(Type type)
    {
        // Arrange & Act
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
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(type);

        // Assert
        Assert.True(modelMetadata.IsComplexType);
    }

    // IsCollectionType / IsEnumerableType

    private class NonCollectionType
    {
    }

    private class DerivedList : List<int>
    {
    }

    private class JustEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public static TheoryData<Type> NonCollectionNonEnumerableData
    {
        get
        {
            return new TheoryData<Type>
                {
                    typeof(object),
                    typeof(int),
                    typeof(NonCollectionType),
                    typeof(string),
                };
        }
    }

    public static TheoryData<Type> CollectionAndEnumerableData
    {
        get
        {
            return new TheoryData<Type>
                {
                    typeof(int[]),
                    typeof(List<string>),
                    typeof(DerivedList),
                    typeof(Collection<int>),
                    typeof(Dictionary<object, object>),
                    typeof(CollectionImplementation),
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonCollectionNonEnumerableData))]
    [InlineData(typeof(IEnumerable))]
    [InlineData(typeof(IEnumerable<string>))]
    [InlineData(typeof(JustEnumerable))]
    public void IsCollectionType_ReturnsFalseForNonCollectionTypes(Type type)
    {
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(type);

        // Assert
        Assert.False(modelMetadata.IsCollectionType);
    }

    [Theory]
    [MemberData(nameof(CollectionAndEnumerableData))]
    public void IsCollectionType_ReturnsTrueForCollectionTypes(Type type)
    {
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(type);

        // Assert
        Assert.True(modelMetadata.IsCollectionType);
    }

    [Theory]
    [MemberData(nameof(NonCollectionNonEnumerableData))]
    public void IsEnumerableType_ReturnsFalseForNonEnumerableTypes(Type type)
    {
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(type);

        // Assert
        Assert.False(modelMetadata.IsEnumerableType);
    }

    [Theory]
    [MemberData(nameof(CollectionAndEnumerableData))]
    [InlineData(typeof(IEnumerable))]
    [InlineData(typeof(IEnumerable<string>))]
    [InlineData(typeof(JustEnumerable))]
    public void IsEnumerableType_ReturnsTrueForEnumerableTypes(Type type)
    {
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(type);

        // Assert
        Assert.True(modelMetadata.IsEnumerableType);
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
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(modelType);

        // Assert
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
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(modelType);

        // Assert
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
        // Arrange & Act
        var modelMetadata = new TestModelMetadata(modelType);

        // Assert
        Assert.Equal(expected, modelMetadata.UnderlyingOrModelType);
    }

    // ElementType

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(int))]
    [InlineData(typeof(NonCollectionType))]
    [InlineData(typeof(string))]
    public void ElementType_ReturnsNull_ForNonCollections(Type modelType)
    {
        // Arrange
        var metadata = new TestModelMetadata(modelType);

        // Act
        var elementType = metadata.ElementType;

        // Assert
        Assert.Null(elementType);
    }

    [Theory]
    [InlineData(typeof(int[]), typeof(int))]
    [InlineData(typeof(List<string>), typeof(string))]
    [InlineData(typeof(DerivedList), typeof(int))]
    [InlineData(typeof(IEnumerable), typeof(object))]
    [InlineData(typeof(IEnumerable<string>), typeof(string))]
    [InlineData(typeof(Collection<int>), typeof(int))]
    [InlineData(typeof(Dictionary<object, object>), typeof(KeyValuePair<object, object>))]
    [InlineData(typeof(DerivedDictionary), typeof(KeyValuePair<string, int>))]
    public void ElementType_ReturnsExpectedMetadata(Type modelType, Type expected)
    {
        // Arrange
        var metadata = new TestModelMetadata(modelType);

        // Act
        var elementType = metadata.ElementType;

        // Assert
        Assert.NotNull(elementType);
        Assert.Equal(expected, elementType);
    }

    // ContainerType

    [Fact]
    public void ContainerType_IsNull_ForType()
    {
        // Arrange & Act
        var metadata = new TestModelMetadata(typeof(int));

        // Assert
        Assert.Null(metadata.ContainerType);
    }

    [Fact]
    public void ContainerType_IsNull_ForParameter()
    {
        // Arrange & Act
        var method = typeof(CollectionImplementation).GetMethod(nameof(CollectionImplementation.Add));
        var parameter = method.GetParameters()[0]; // Add(string item)
        var metadata = new TestModelMetadata(parameter);

        // Assert
        Assert.Null(metadata.ContainerType);
    }

    [Fact]
    public void ContainerType_ReturnExpectedMetadata_ForProperty()
    {
        // Arrange & Act
        var property = typeof(string).GetProperty(nameof(string.Length));
        var metadata = new TestModelMetadata(property, typeof(int), typeof(string));

        // Assert
        Assert.Equal(typeof(string), metadata.ContainerType);
    }

    // Name / ParameterName / PropertyName

    [Fact]
    public void Names_ReturnExpectedMetadata_ForType()
    {
        // Arrange & Act
        var metadata = new TestModelMetadata(typeof(int));

        // Assert
        Assert.Null(metadata.Name);
        Assert.Null(metadata.ParameterName);
        Assert.Null(metadata.PropertyName);
    }

    [Fact]
    public void Names_ReturnExpectedMetadata_ForParameter()
    {
        // Arrange & Act
        var method = typeof(CollectionImplementation).GetMethod(nameof(CollectionImplementation.Add));
        var parameter = method.GetParameters()[0]; // Add(string item)
        var metadata = new TestModelMetadata(parameter);

        // Assert
        Assert.Equal("item", metadata.Name);
        Assert.Equal("item", metadata.ParameterName);
        Assert.Null(metadata.PropertyName);
    }

    [Fact]
    public void Names_ReturnExpectedMetadata_ForProperty()
    {
        // Arrange & Act
        var property = typeof(string).GetProperty(nameof(string.Length));
        var metadata = new TestModelMetadata(property, typeof(int), typeof(string));

        // Assert
        Assert.Equal(nameof(string.Length), metadata.Name);
        Assert.Null(metadata.ParameterName);
        Assert.Equal(nameof(string.Length), metadata.PropertyName);
    }

    // GetDisplayName()

    [Fact]
    public void GetDisplayName_ReturnsDisplayName_IfSet()
    {
        // Arrange
        var property = typeof(string).GetProperty(nameof(string.Length));
        var metadata = new TestModelMetadata(property, typeof(int), typeof(string));
        metadata.SetDisplayName("displayName");

        // Act
        var result = metadata.GetDisplayName();

        // Assert
        Assert.Equal("displayName", result);
    }

    [Fact]
    public void GetDisplayName_ReturnsParameterName_WhenSetAndDisplayNameIsNull()
    {
        // Arrange
        var method = typeof(CollectionImplementation).GetMethod(nameof(CollectionImplementation.Add));
        var parameter = method.GetParameters()[0]; // Add(string item)
        var metadata = new TestModelMetadata(parameter);

        // Act
        var result = metadata.GetDisplayName();

        // Assert
        Assert.Equal("item", result);
    }

    [Fact]
    public void GetDisplayName_ReturnsPropertyName_WhenSetAndDisplayNameIsNull()
    {
        // Arrange
        var property = typeof(string).GetProperty(nameof(string.Length));
        var metadata = new TestModelMetadata(property, typeof(int), typeof(string));

        // Act
        var result = metadata.GetDisplayName();

        // Assert
        Assert.Equal("Length", result);
    }

    [Fact]
    public void GetDisplayName_ReturnsTypeName_WhenPropertyNameAndDisplayNameAreNull()
    {
        // Arrange
        var metadata = new TestModelMetadata(typeof(string));

        // Act
        var result = metadata.GetDisplayName();

        // Assert
        Assert.Equal("String", result);
    }

    // Virtual methods and properties that throw NotImplementedException in the abstract class.

    [Fact]
    public void GetContainerMetadata_ThrowsNotImplementedException_ByDefault()
    {
        // Arrange
        var metadata = new TestModelMetadata(typeof(DerivedList));

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => metadata.ContainerMetadata);
    }

    [Fact]
    public void GetMetadataForType_ByDefaultThrows_NotImplementedException()
    {
        // Arrange
        var metadata = new TestModelMetadata(typeof(string));

        // Act & Assert
        var result = Assert.Throws<NotImplementedException>(() => metadata.GetMetadataForType(typeof(string)));
    }

    [Fact]
    public void GetMetadataForProperties_ByDefaultThrows_NotImplementedException()
    {
        // Arrange
        var metadata = new TestModelMetadata(typeof(string));

        // Act & Assert
        var result = Assert.Throws<NotImplementedException>(() => metadata.GetMetadataForProperties(typeof(string)));
    }

    [Fact]
    public void DynamicPropertiesThrowWhenIsDynamicCodeSupportedIsTrue()
    {
        var options = new RemoteInvokeOptions();

        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported", false);
        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var metadata = new TestModelMetadata(typeof(DateTime));
            Assert.Throws<NotSupportedException>(() => metadata.ElementType);
            Assert.Throws<NotSupportedException>(() => metadata.IsParseableType);
            Assert.Throws<NotSupportedException>(() => metadata.IsConvertibleType);
            Assert.Throws<NotSupportedException>(() => metadata.IsComplexType);
            Assert.Throws<NotSupportedException>(() => metadata.IsCollectionType);
        }, options);
    }

    [Fact]
    public void DynamicPropertiesSetWhenIsDynamicCodeSupportedIsTrue()
    {
        var options = new RemoteInvokeOptions();

        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported", true);
        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var metadata = new TestModelMetadata(typeof(DateTime));
            Assert.Null(metadata.ElementType);
            Assert.True(metadata.IsParseableType);
            Assert.False(metadata.IsCollectionType);
            Assert.True(metadata.IsConvertibleType);
            Assert.False(metadata.IsComplexType);
        }, options);
    }

    private class TestModelMetadata : ModelMetadata
    {
        private string _displayName;

        public TestModelMetadata(Type modelType)
            : base(ModelMetadataIdentity.ForType(modelType))
        {
        }

        public TestModelMetadata(ParameterInfo parameter)
            : base(ModelMetadataIdentity.ForParameter(parameter))
        {
        }

        public TestModelMetadata(PropertyInfo propertyInfo, Type modelType, Type containerType)
            : base(ModelMetadataIdentity.ForProperty(propertyInfo, modelType, containerType))
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

        public override IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues
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

        public override ModelBindingMessageProvider ModelBindingMessageProvider
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

        public override string Placeholder
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

        public override IPropertyFilterProvider PropertyFilterProvider
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

        public override IPropertyValidationFilter PropertyValidationFilter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool ValidateChildren
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

        public override ModelMetadata BoundConstructor => throw new NotImplementedException();

        public override Func<object[], object> BoundConstructorInvoker => throw new NotImplementedException();

        public override IReadOnlyList<ModelMetadata> BoundConstructorParameters => throw new NotImplementedException();
    }

    private class CollectionImplementation : ICollection<string>
    {
        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(string item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(string item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(string item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedDictionary : Dictionary<string, int>
    {
    }
}
