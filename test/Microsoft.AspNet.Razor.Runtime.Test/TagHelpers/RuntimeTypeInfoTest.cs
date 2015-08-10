// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class RuntimeTypeInfoTest
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(Tuple<,>))]
        [InlineData(typeof(IDictionary<string, string>))]
        [InlineData(typeof(IDictionary<string, IDictionary<string, CustomType>>))]
        [InlineData(typeof(AbstractType))]
        [InlineData(typeof(PrivateType))]
        [InlineData(typeof(KnownKeyDictionary<>))]
        [InlineData(typeof(KnownKeyDictionary<string>))]
        public void RuntimeTypeInfo_ReturnsMetadataOfAdaptingType(Type type)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();
            var runtimeTypeInfo = new RuntimeTypeInfo(typeInfo);

            // Act and Assert
            Assert.Same(runtimeTypeInfo.TypeInfo, typeInfo);
            Assert.Equal(runtimeTypeInfo.Name, typeInfo.Name);
            Assert.Equal(runtimeTypeInfo.FullName, typeInfo.FullName);
            Assert.Equal(runtimeTypeInfo.IsAbstract, typeInfo.IsAbstract);
            Assert.Equal(runtimeTypeInfo.IsGenericType, typeInfo.IsGenericType);
            Assert.Equal(runtimeTypeInfo.IsPublic, typeInfo.IsPublic);
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
        [InlineData(typeof(Dictionary<,>), new Type[] { null, null })]
        [InlineData(typeof(KnownKeyDictionary<>), new[] { typeof(string), null })]
        [InlineData(typeof(KnownKeyDictionary<ImplementsIDictionary>),
            new[] { typeof(string), typeof(ImplementsIDictionary) })]
        public void GetGenericDictionaryParameterNames_ReturnsKeyAndValueParameterTypeNames(
            Type type,
            Type[] expectedTypes)
        {
            // Arrange
            var runtimeTypeInfo = new RuntimeTypeInfo(type.GetTypeInfo());
            var expected = expectedTypes.Select(t => t?.FullName);

            // Act
            var actual = runtimeTypeInfo.GetGenericDictionaryParameterNames();

            // Assert
            Assert.Equal(expected, actual);
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
            var actual = runtimeTypeInfo.GetGenericDictionaryParameterNames();

            // Assert
            Assert.Null(actual);
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
    }
}
