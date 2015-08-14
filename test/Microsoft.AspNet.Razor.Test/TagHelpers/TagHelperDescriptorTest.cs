// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperDescriptorTest
    {
        [Fact]
        public void TagHelperDescriptor_CanBeSerialized()
        {
            // Arrange
            var descriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName =  "assembly name",
                RequiredAttributes = new[] { "required attribute one", "required attribute two" },
                AllowedChildren = new[] { "allowed child one" },
                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                {
                    Summary = "usage summary",
                    Remarks = "usage remarks",
                    OutputElementHint = "some-tag"
                }
            };

            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":" +
                "[\"required attribute one\",\"required attribute two\"]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":[\"allowed child one\"]," +
                $"\"{ nameof(TagHelperDescriptor.TagStructure) }\":0," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":{{"+
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Summary) }\":\"usage summary\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Remarks) }\":\"usage remarks\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.OutputElementHint) }\":\"some-tag\"}}}}";

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(descriptor);

            // Assert
            Assert.Equal(expectedSerializedDescriptor, serializedDescriptor, StringComparer.Ordinal);
        }

        [Fact]
        public void TagHelperDescriptor_WithAttributes_CanBeSerialized()
        {
            // Arrange
            var descriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName = "assembly name",
                Attributes = new[]
                {
                   new TagHelperAttributeDescriptor
                   {
                        Name = "attribute one",
                        PropertyName = "property name",
                        TypeName = "property type name"
                   },
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute two",
                        PropertyName = "property name",
                        TypeName = typeof(string).FullName
                    },
                },
                TagStructure = TagStructure.NormalOrSelfClosing
            };

            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[" +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":null," +
                $"\"{ nameof(TagHelperDescriptor.TagStructure) }\":1," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(descriptor);

            // Assert
            Assert.Equal(expectedSerializedDescriptor, serializedDescriptor, StringComparer.Ordinal);
        }

        [Fact]
        public void TagHelperDescriptor_WithIndexerAttributes_CanBeSerialized()
        {
            // Arrange
            var descriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName = "assembly name",
                Attributes = new[]
                {
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute one",
                        PropertyName = "property name",
                        TypeName = "property type name",
                        IsIndexer = true
                    },
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute two",
                        PropertyName = "property name",
                        TypeName = typeof(string).FullName,
                        IsIndexer = true
                    },
                },
                AllowedChildren = new[] { "allowed child one", "allowed child two" }
            };

            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[" +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":[\"allowed child one\",\"allowed child two\"]," +
                $"\"{ nameof(TagHelperDescriptor.TagStructure) }\":0," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(descriptor);

            // Assert
            Assert.Equal(expectedSerializedDescriptor, serializedDescriptor, StringComparer.Ordinal);
        }

        [Fact]
        public void TagHelperDescriptor_CanBeDeserialized()
        {
            // Arrange
            var serializedDescriptor =
                $"{{\"{nameof(TagHelperDescriptor.Prefix)}\":\"prefix:\"," +
                $"\"{nameof(TagHelperDescriptor.TagName)}\":\"tag name\"," +
                $"\"{nameof(TagHelperDescriptor.FullTagName)}\":\"prefix:tag name\"," +
                $"\"{nameof(TagHelperDescriptor.TypeName)}\":\"type name\"," +
                $"\"{nameof(TagHelperDescriptor.AssemblyName)}\":\"assembly name\"," +
                $"\"{nameof(TagHelperDescriptor.Attributes)}\":[]," +
                $"\"{nameof(TagHelperDescriptor.RequiredAttributes)}\":" +
                "[\"required attribute one\",\"required attribute two\"]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":[\"allowed child one\",\"allowed child two\"]," +
                $"\"{nameof(TagHelperDescriptor.TagStructure)}\":2," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":{{" +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Summary) }\":\"usage summary\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Remarks) }\":\"usage remarks\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.OutputElementHint) }\":\"some-tag\"}}}}";
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName = "assembly name",
                RequiredAttributes = new[] { "required attribute one", "required attribute two" },
                AllowedChildren = new[] { "allowed child one", "allowed child two" },
                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                {
                    Summary = "usage summary",
                    Remarks = "usage remarks",
                    OutputElementHint = "some-tag"
                }
            };

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(expectedDescriptor.Prefix, descriptor.Prefix, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TagName, descriptor.TagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.FullTagName, descriptor.FullTagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TypeName, descriptor.TypeName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.AssemblyName, descriptor.AssemblyName, StringComparer.Ordinal);
            Assert.Empty(descriptor.Attributes);
            Assert.Equal(expectedDescriptor.RequiredAttributes, descriptor.RequiredAttributes, StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.DesignTimeDescriptor,
                descriptor.DesignTimeDescriptor,
                TagHelperDesignTimeDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_WithAttributes_CanBeDeserialized()
        {
            // Arrange
            var serializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[" +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":null," +
                $"\"{nameof(TagHelperDescriptor.TagStructure)}\":0," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName = "assembly name",
                Attributes = new[]
                {
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute one",
                        PropertyName = "property name",
                        TypeName = "property type name"
                    },
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute two",
                        PropertyName = "property name",
                        TypeName = typeof(string).FullName
                    },
                },
                AllowedChildren = new[] { "allowed child one", "allowed child two" }
            };

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(expectedDescriptor.Prefix, descriptor.Prefix, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TagName, descriptor.TagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.FullTagName, descriptor.FullTagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TypeName, descriptor.TypeName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.AssemblyName, descriptor.AssemblyName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.Attributes, descriptor.Attributes, TagHelperAttributeDescriptorComparer.Default);
            Assert.Empty(descriptor.RequiredAttributes);
        }

        [Fact]
        public void TagHelperDescriptor_WithIndexerAttributes_CanBeDeserialized()
        {
            // Arrange
            var serializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[" +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsIndexer) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.DesignTimeDescriptor) }\":null}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.AllowedChildren) }\":null," +
                $"\"{nameof(TagHelperDescriptor.TagStructure)}\":1," +
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = "prefix:",
                TagName = "tag name",
                TypeName = "type name",
                AssemblyName = "assembly name",
                Attributes = new[]
                {
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute one",
                        PropertyName = "property name",
                        TypeName = "property type name",
                        IsIndexer = true
                    },
                    new TagHelperAttributeDescriptor
                   {
                        Name = "attribute two",
                        PropertyName = "property name",
                        TypeName = typeof(string).FullName,
                        IsIndexer = true
                    }
                },
                TagStructure = TagStructure.NormalOrSelfClosing
            };

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(expectedDescriptor.Prefix, descriptor.Prefix, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TagName, descriptor.TagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.FullTagName, descriptor.FullTagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TypeName, descriptor.TypeName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.AssemblyName, descriptor.AssemblyName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.Attributes, descriptor.Attributes, TagHelperAttributeDescriptorComparer.Default);
            Assert.Empty(descriptor.RequiredAttributes);
        }
    }
}
