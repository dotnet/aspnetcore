// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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
            var descriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                requiredAttributes: new[] { "required attribute one", "required attribute two" },
                designTimeDescriptor: new TagHelperDesignTimeDescriptor("usage summary", "usage remarks", "some-tag"));

            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":" +
                "[\"required attribute one\",\"required attribute two\"]," +
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
            var descriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new[]
                {
                    new TagHelperAttributeDescriptor(
                        name: "attribute one",
                        propertyName: "property name",
                        typeName: "property type name",
                        isIndexer: false,
                        designTimeDescriptor: null),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName,
                        isIndexer: false,
                        designTimeDescriptor: null),
                },
                requiredAttributes: Enumerable.Empty<string>(),
                designTimeDescriptor: null);
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
            var descriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new[]
                {
                    new TagHelperAttributeDescriptor(
                        name: "attribute one",
                        propertyName: "property name",
                        typeName: "property type name",
                        isIndexer: true,
                        designTimeDescriptor: null),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName,
                        isIndexer: true,
                        designTimeDescriptor: null),
                },
                requiredAttributes: Enumerable.Empty<string>(),
                designTimeDescriptor: null);
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
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":{{" +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Summary) }\":\"usage summary\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.Remarks) }\":\"usage remarks\"," +
                $"\"{ nameof(TagHelperDesignTimeDescriptor.OutputElementHint) }\":\"some-tag\"}}}}";
            var expectedDescriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                requiredAttributes: new[] { "required attribute one", "required attribute two" },
                designTimeDescriptor: new TagHelperDesignTimeDescriptor("usage summary", "usage remarks", "some-tag"));

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
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";
            var expectedDescriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new[]
                {
                    new TagHelperAttributeDescriptor(
                        name: "attribute one",
                        propertyName: "property name",
                        typeName: "property type name",
                        isIndexer: false,
                        designTimeDescriptor: null),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName,
                        isIndexer: false,
                        designTimeDescriptor: null),
                },
                requiredAttributes: Enumerable.Empty<string>(),
                designTimeDescriptor: null);

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(expectedDescriptor.Prefix, descriptor.Prefix, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TagName, descriptor.TagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.FullTagName, descriptor.FullTagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TypeName, descriptor.TypeName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.AssemblyName, descriptor.AssemblyName, StringComparer.Ordinal);

            Assert.Equal(2, descriptor.Attributes.Count);
            Assert.Equal(expectedDescriptor.Attributes[0].IsIndexer, descriptor.Attributes[0].IsIndexer);
            Assert.Equal(expectedDescriptor.Attributes[0].IsStringProperty, descriptor.Attributes[0].IsStringProperty);
            Assert.Equal(expectedDescriptor.Attributes[0].Name, descriptor.Attributes[0].Name, StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[0].PropertyName,
                descriptor.Attributes[0].PropertyName,
                StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[0].TypeName,
                descriptor.Attributes[0].TypeName,
                StringComparer.Ordinal);

            Assert.Equal(expectedDescriptor.Attributes[1].IsIndexer, descriptor.Attributes[1].IsIndexer);
            Assert.Equal(expectedDescriptor.Attributes[1].IsStringProperty, descriptor.Attributes[1].IsStringProperty);
            Assert.Equal(expectedDescriptor.Attributes[1].Name, descriptor.Attributes[1].Name, StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[1].PropertyName,
                descriptor.Attributes[1].PropertyName,
                StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[1].TypeName,
                descriptor.Attributes[1].TypeName,
                StringComparer.Ordinal);

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
                $"\"{ nameof(TagHelperDescriptor.DesignTimeDescriptor) }\":null}}";
            var expectedDescriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new[]
                {
                    new TagHelperAttributeDescriptor(
                        name: "attribute one",
                        propertyName: "property name",
                        typeName: "property type name",
                        isIndexer: true,
                        designTimeDescriptor: null),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName,
                        isIndexer: true,
                        designTimeDescriptor: null),
                },
                requiredAttributes: Enumerable.Empty<string>(),
                designTimeDescriptor: null);

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor);

            // Assert
            Assert.NotNull(descriptor);
            Assert.Equal(expectedDescriptor.Prefix, descriptor.Prefix, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TagName, descriptor.TagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.FullTagName, descriptor.FullTagName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.TypeName, descriptor.TypeName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor.AssemblyName, descriptor.AssemblyName, StringComparer.Ordinal);

            Assert.Equal(2, descriptor.Attributes.Count);
            Assert.Equal(expectedDescriptor.Attributes[0].IsIndexer, descriptor.Attributes[0].IsIndexer);
            Assert.Equal(expectedDescriptor.Attributes[0].IsStringProperty, descriptor.Attributes[0].IsStringProperty);
            Assert.Equal(expectedDescriptor.Attributes[0].Name, descriptor.Attributes[0].Name, StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[0].PropertyName,
                descriptor.Attributes[0].PropertyName,
                StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[0].TypeName,
                descriptor.Attributes[0].TypeName,
                StringComparer.Ordinal);

            Assert.Equal(expectedDescriptor.Attributes[1].IsIndexer, descriptor.Attributes[1].IsIndexer);
            Assert.Equal(expectedDescriptor.Attributes[1].IsStringProperty, descriptor.Attributes[1].IsStringProperty);
            Assert.Equal(expectedDescriptor.Attributes[1].Name, descriptor.Attributes[1].Name, StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[1].PropertyName,
                descriptor.Attributes[1].PropertyName,
                StringComparer.Ordinal);
            Assert.Equal(
                expectedDescriptor.Attributes[1].TypeName,
                descriptor.Attributes[1].TypeName,
                StringComparer.Ordinal);

            Assert.Empty(descriptor.RequiredAttributes);
        }
    }
}
