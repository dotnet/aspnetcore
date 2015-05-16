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
                requiredAttributes: new[] { "required attribute one", "required attribute two" });
            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":" +
                "[\"required attribute one\",\"required attribute two\"]}";

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
                        typeName: "property type name"),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName),
                },
                requiredAttributes: Enumerable.Empty<string>());
            var expectedSerializedDescriptor =
                $"{{\"{ nameof(TagHelperDescriptor.Prefix) }\":\"prefix:\"," +
                $"\"{ nameof(TagHelperDescriptor.TagName) }\":\"tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.FullTagName) }\":\"prefix:tag name\"," +
                $"\"{ nameof(TagHelperDescriptor.TypeName) }\":\"type name\"," +
                $"\"{ nameof(TagHelperDescriptor.AssemblyName) }\":\"assembly name\"," +
                $"\"{ nameof(TagHelperDescriptor.Attributes) }\":[" +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]}}";

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
                "[\"required attribute one\",\"required attribute two\"]}";
            var expectedDescriptor = new TagHelperDescriptor(
                prefix: "prefix:",
                tagName: "tag name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                requiredAttributes: new[] { "required attribute one", "required attribute two" });

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
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":false," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute one\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"property type name\"}}," +
                $"{{\"{ nameof(TagHelperAttributeDescriptor.IsStringProperty) }\":true," +
                $"\"{ nameof(TagHelperAttributeDescriptor.Name) }\":\"attribute two\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.PropertyName) }\":\"property name\"," +
                $"\"{ nameof(TagHelperAttributeDescriptor.TypeName) }\":\"{ typeof(string).FullName }\"}}]," +
                $"\"{ nameof(TagHelperDescriptor.RequiredAttributes) }\":[]}}";
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
                        typeName: "property type name"),
                    new TagHelperAttributeDescriptor(
                        name: "attribute two",
                        propertyName: "property name",
                        typeName: typeof(string).FullName),
                },
                requiredAttributes: Enumerable.Empty<string>());

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
