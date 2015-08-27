// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class RuntimeTypeInfoTest
    {
        private static readonly string StringFullName = typeof(string).FullName;
        private static readonly string CollectionsNamespace = typeof(IDictionary<,>).Namespace;

        public static TheoryData RuntimeTypeInfo_ReturnsMetadataOfAdaptingTypeData =>
            new TheoryData<Type, string>
            {
                { typeof(int), typeof(int).FullName },
                { typeof(string), typeof(string).FullName },
                { typeof(Tuple<>), typeof(Tuple<>).FullName },
                { typeof(Tuple<,>), typeof(Tuple<,>).FullName },
                {
                    typeof(IDictionary<string, string>),
                    $"{typeof(IDictionary<,>).FullName}[[{StringFullName}],[{StringFullName}]]"
                },
                {
                    typeof(IDictionary<string, IDictionary<string, CustomType>>),
                    $"{typeof(IDictionary<,>).FullName}[[{StringFullName}],[{typeof(IDictionary<,>).FullName}" +
                    $"[[{StringFullName}],[{typeof(CustomType).FullName}]]]]"
                },
                {
                    typeof(IList<IReadOnlyList<IDictionary<List<string>, Tuple<CustomType, object[]>>>>),
                    $"{typeof(IList<>).FullName}[[{typeof(IReadOnlyList<>).FullName}[[{typeof(IDictionary<,>).FullName}[[" +
                    $"{typeof(List<>).FullName}[[{StringFullName}]]],[{typeof(Tuple<,>).FullName}[[{typeof(CustomType).FullName}]," +
                    $"[{typeof(object).FullName}[]]]]]]]]]"
                },
                { typeof(AbstractType), typeof(AbstractType).FullName },
                { typeof(PrivateType), typeof(PrivateType).FullName },
                { typeof(KnownKeyDictionary<>), typeof(KnownKeyDictionary<>).FullName },
                {
                    typeof(KnownKeyDictionary<string>),
                    $"{typeof(KnownKeyDictionary<>).Namespace}" +
                    $".RuntimeTypeInfoTest+KnownKeyDictionary`1[[{StringFullName}]]"
                }
            };

        [Theory]
        [MemberData(nameof(RuntimeTypeInfo_ReturnsMetadataOfAdaptingTypeData))]
        public void RuntimeTypeInfo_ReturnsMetadataOfAdaptingType(Type type, string expectedFullName)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();
            var runtimeTypeInfo = new RuntimeTypeInfo(typeInfo);

            // Act and Assert
            Assert.Same(typeInfo, runtimeTypeInfo.TypeInfo);
            Assert.Equal(typeInfo.Name, runtimeTypeInfo.Name);
            Assert.Equal(expectedFullName, runtimeTypeInfo.FullName);
            Assert.Equal(typeInfo.IsAbstract, runtimeTypeInfo.IsAbstract);
            Assert.Equal(typeInfo.IsGenericType, runtimeTypeInfo.IsGenericType);
            Assert.Equal(typeInfo.IsPublic, runtimeTypeInfo.IsPublic);
        }

        [Fact]
        public void Properties_ReturnsPublicPropertiesOfAdaptingType()
        {
            // Arrange
            var typeInfo = typeof(SubType).GetTypeInfo();
            var runtimeTypeInfo = new RuntimeTypeInfo(typeInfo);

            // Act and Assert
            Assert.Collection(runtimeTypeInfo.Properties,
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("Property1", property.Name);
                },
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("Property2", property.Name);
                },
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("Property3", property.Name);
                },
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("Property4", property.Name);
                },
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("BaseTypeProperty", property.Name);
                },
                property =>
                {
                    Assert.IsType<RuntimePropertyInfo>(property);
                    Assert.Equal("ProtectedProperty", property.Name);
                });
        }

        [Fact]
        public void GetCustomAttributes_ReturnsAllAttributesOfType()
        {
            // Arrange
            var typeInfo = typeof(TypeWithAttributes).GetTypeInfo();
            var runtimeTypeInfo = new RuntimeTypeInfo(typeInfo);
            var expected = typeInfo.GetCustomAttributes<TargetElementAttribute>();

            // Act
            var actual = runtimeTypeInfo.GetCustomAttributes<TargetElementAttribute>();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetCustomAttributes_DoesNotInheritAttributesFromBaseType()
        {
            // Arrange
            var typeInfo = typeof(SubType).GetTypeInfo();
            var runtimeTypeInfo = new RuntimeTypeInfo(typeInfo);

            // Act
            var actual = runtimeTypeInfo.GetCustomAttributes<EditorBrowsableAttribute>();

            // Assert
            Assert.Empty(actual);
        }

        [Theory]
        [InlineData(typeof(ITagHelper))]
        [InlineData(typeof(TagHelper))]
        [InlineData(typeof(ImplementsITagHelper))]
        [InlineData(typeof(DerivesFromTagHelper))]
        [InlineData(typeof(Fake.ImplementsRealITagHelper))]
        public void IsTagHelper_ReturnsTrueIfTypeImplementsTagHelper(Type type)
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(type.GetTypeInfo());

            // Act
            var result = runtimeTypeInfo.IsTagHelper;

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(SubType))]
        [InlineData(typeof(Fake.DoesNotImplementRealITagHelper))]
        public void IsTagHelper_ReturnsFalseIfTypeDoesNotImplementTagHelper(Type type)
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(type.GetTypeInfo());

            // Act
            var result = runtimeTypeInfo.IsTagHelper;

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(Dictionary<string, string>), new[] { typeof(string), typeof(string) })]
        [InlineData(typeof(DerivesFromDictionary), new[] { typeof(int), typeof(object) })]
        [InlineData(typeof(ImplementsIDictionary), new[] { typeof(List<string>), typeof(string) })]
        [InlineData(typeof(IDictionary<string, IDictionary<string, CustomType>>),
            new[] { typeof(string), typeof(IDictionary<string, CustomType>) })]
        [InlineData(typeof(KnownKeyDictionary<ImplementsIDictionary>),
            new[] { typeof(string), typeof(ImplementsIDictionary) })]
        public void GetGenericDictionaryParameters_ReturnsKeyAndValueParameterTypeNames(
            Type type,
            Type[] expectedTypes)
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(type.GetTypeInfo());

            // Act
            var actual = runtimeTypeInfo.GetGenericDictionaryParameters();

            // Assert
            Assert.Collection(actual,
                keyType =>
                {
                    Assert.Equal(new RuntimeTypeInfo(expectedTypes[0].GetTypeInfo()), keyType);
                },
                valueType =>
                {
                    Assert.Equal(new RuntimeTypeInfo(expectedTypes[1].GetTypeInfo()), valueType);
                });
        }

        [Fact]
        public void GetGenericDictionaryParameters_WorksWhenValueParameterIsOpen()
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(typeof(KnownKeyDictionary<>).GetTypeInfo());

            // Act
            var actual = runtimeTypeInfo.GetGenericDictionaryParameters();

            // Assert
            Assert.Collection(actual,
                keyType =>
                {
                    Assert.Equal(new RuntimeTypeInfo(typeof(string).GetTypeInfo()), keyType);
                },
                valueType =>
                {
                    Assert.Null(valueType);
                });
        }

        [Fact]
        public void GetGenericDictionaryParameters_WorksWhenKeyAndValueParametersAreOpen()
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(typeof(Dictionary<,>).GetTypeInfo());

            // Act
            var actual = runtimeTypeInfo.GetGenericDictionaryParameters();

            // Assert
            Assert.Equal(new RuntimeTypeInfo[] { null, null }, actual);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(ITagHelper))]
        public void GetGenericDictionaryParameterNames_ReturnsNullIfTypeDoesNotImplementGenericDictionary(Type type)
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(type.GetTypeInfo());

            // Act
            var actual = runtimeTypeInfo.GetGenericDictionaryParameters();

            // Assert
            Assert.Null(actual);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(IDictionary<,>))]
        [InlineData(typeof(ITagHelper))]
        [InlineData(typeof(TagHelper))]
        public void Equals_ReturnsTrueIfTypeInfosAreIdentical(Type type)
        {
            // Arrange
            var typeA = new RuntimeTypeInfo(type.GetTypeInfo());
            var typeB = new RuntimeTypeInfo(type.GetTypeInfo());

            // Act
            var equals = typeA.Equals(typeB);
            var hashCodeA = typeA.GetHashCode();
            var hashCodeB = typeB.GetHashCode();

            // Assert
            Assert.True(equals);
            Assert.Equal(hashCodeA, hashCodeB);
        }

        public static TheoryData Equals_ReturnsTrueIfTypeFullNamesAreIdenticalData =>
            new TheoryData<Type, string>
            {
                { typeof(string), typeof(string).FullName },
                { typeof(ITagHelper), typeof(ITagHelper).FullName },
                { typeof(TagHelper), typeof(TagHelper).FullName },
                { typeof(TagHelper), typeof(TagHelper).FullName },
                { typeof(IDictionary<,>), typeof(IDictionary<,>).FullName },
                {
                    typeof(IDictionary<string,string>),
                    RuntimeTypeInfo.SanitizeFullName(typeof(IDictionary<string, string>).FullName)
                },
            };

        [Theory]
        [MemberData(nameof(Equals_ReturnsTrueIfTypeFullNamesAreIdenticalData))]
        public void Equals_ReturnsTrueIfTypeInfoNamesAreIdentical(Type type, string fullName)
        {
            // Arrange
            var typeA = new RuntimeTypeInfo(type.GetTypeInfo());
            var typeB = new TestTypeInfo
            {
                FullName = fullName
            };

            // Act
            var equals = typeA.Equals(typeB);

            // Assert
            Assert.True(equals);
        }

        [Theory]
        [InlineData(typeof(string), typeof(object))]
        [InlineData(typeof(IDictionary<,>), typeof(IDictionary<string, string>))]
        [InlineData(typeof(KnownKeyDictionary<string>), typeof(IDictionary<string, string>))]
        [InlineData(typeof(ITagHelper), typeof(TagHelper))]
        public void Equals_ReturnsFalseIfTypeInfosAreDifferent(Type typeA, Type typeB)
        {
            // Arrange
            var typeAInfo = new RuntimeTypeInfo(typeA.GetTypeInfo());
            var typeBInfo = new RuntimeTypeInfo(typeB.GetTypeInfo());

            // Act
            var equals = typeAInfo.Equals(typeBInfo);
            var hashCodeA = typeAInfo.GetHashCode();
            var hashCodeB = typeBInfo.GetHashCode();

            // Assert
            Assert.False(equals);
            Assert.NotEqual(hashCodeA, hashCodeB);
        }

        [Theory]
        [MemberData(nameof(Equals_ReturnsTrueIfTypeFullNamesAreIdenticalData))]
        public void Equals_ReturnsFalseIfTypeInfoNamesAreDifferent(Type type, string fullName)
        {
            // Arrange
            var typeA = new RuntimeTypeInfo(type.GetTypeInfo());
            var typeB = new TestTypeInfo
            {
                FullName = "Different" + fullName
            };

            // Act
            var equals = typeA.Equals(typeB);

            // Assert
            Assert.False(equals);
        }

        public class AbstractType
        {
        }

        internal class InternalType
        {
        }

        private class PrivateType
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private class BaseType
        {
            public string this[string key]
            {
                get { return ""; }
                set { }
            }

            public string BaseTypeProperty { get; set; }

            protected int ProtectedProperty { get; set; }
        }

        private class SubType : BaseType
        {
            public string Property1 { get; set; }

            public int Property2 { get; }

            public object Property3 { private get; set; }

            private int Property4 { get; set; }
        }

        [TargetElement("test1")]
        [TargetElement("test2")]
        private class TypeWithAttributes
        {
        }

        private class DerivesFromTagHelper : TagHelper
        {
        }

        private class ImplementsITagHelper : ITagHelper
        {
            public int Order { get; } = 0;

            public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
            {
                throw new NotImplementedException();
            }
        }

        private class DerivesFromDictionary : Dictionary<int, object>
        {
        }

        private class ImplementsIDictionary : IDictionary<List<string>, string>, IReadOnlyList<int>
        {
            public int this[int index]
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string this[List<string> key]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

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

            public ICollection<List<string>> Keys
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ICollection<string> Values
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Add(KeyValuePair<List<string>, string> item)
            {
                throw new NotImplementedException();
            }

            public void Add(List<string> key, string value)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<List<string>, string> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(List<string> key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<List<string>, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<List<string>, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<List<string>, string> item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(List<string> key)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(List<string> key, out string value)
            {
                throw new NotImplementedException();
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public class KnownKeyDictionary<TValue> : Dictionary<string, TValue>
        {
        }

        private class CustomType
        {
        }

        private class TestTypeInfo : ITypeInfo
        {
            public string FullName { get; set; }

            public bool IsAbstract
            {
                get
                {
                    throw new NotImplementedException();
    }
}

            public bool IsGenericType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsPublic
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsTagHelper
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IEnumerable<IPropertyInfo> Properties
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>() where TAttribute : Attribute
            {
                throw new NotImplementedException();
            }

            public ITypeInfo[] GetGenericDictionaryParameters()
            {
                throw new NotImplementedException();
            }
        }
    }
}