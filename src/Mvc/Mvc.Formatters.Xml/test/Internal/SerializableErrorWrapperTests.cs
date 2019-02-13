// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    public class SerializableErrorWrapperTests
    {
        [Fact]
        public void DefaultConstructor_CreatesSerializableError()
        {
            // Arrange and Act
            var wrapper = new SerializableErrorWrapper();

            // Assert
            Assert.NotNull(wrapper.SerializableError);
            Assert.Empty(wrapper.SerializableError);
        }

        [Fact]
        public void WrappedSerializableErrorInstance_ReturnedFromProperty()
        {
            // Arrange
            var serializableError = new SerializableError();
            serializableError.Add("key1", "key1-error");

            // Act
            var wrapper = new SerializableErrorWrapper(serializableError);

            // Assert
            Assert.NotNull(wrapper.SerializableError);
            Assert.Same(serializableError, wrapper.SerializableError);
            Assert.Single(wrapper.SerializableError);
            Assert.True(wrapper.SerializableError.ContainsKey("key1"));
            Assert.Equal("key1-error", wrapper.SerializableError["key1"]);
        }

        [Fact]
        public void GetSchema_Returns_Null()
        {
            // Arrange
            var serializableError = new SerializableErrorWrapper(new SerializableError(new ModelStateDictionary()));

            // Act & Assert
            Assert.Null(serializableError.GetSchema());
        }

        [Fact]
        public void ReadXml_ReadsSerializableErrorXml()
        {
            // Arrange
            var serializableErrorXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Error><key1>Test Error 1 Test Error 2</key1><key2>Test Error 3</key2></Error>";
            var serializer = new DataContractSerializer(typeof(SerializableErrorWrapper));

            // Act
            var wrapper = (SerializableErrorWrapper)serializer.ReadObject(
                new MemoryStream(Encoding.UTF8.GetBytes(serializableErrorXml)));
            var errors = wrapper.SerializableError;

            // Assert
            Assert.Equal("Test Error 1 Test Error 2", errors["key1"]);
            Assert.Equal("Test Error 3", errors["key2"]);
        }

        [Fact]
        public void WriteXml_WritesValidXml()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("key1", "Test Error 1");
            modelState.AddModelError("key1", "Test Error 2");
            modelState.AddModelError("key2", "Test Error 3");
            var serializableError = new SerializableError(modelState);
            var outputStream = new MemoryStream();

            // Act
            using (var xmlWriter = XmlWriter.Create(outputStream))
            {
                var dataContractSerializer = new DataContractSerializer(typeof(SerializableErrorWrapper));
                dataContractSerializer.WriteObject(xmlWriter, new SerializableErrorWrapper(serializableError));
            }
            outputStream.Position = 0;
            var res = new StreamReader(outputStream, Encoding.UTF8).ReadToEnd();

            // Assert
            var expectedContent =
                TestPlatformHelper.IsMono ?
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><Error xmlns:i=\"" +
                    "http://www.w3.org/2001/XMLSchema-instance\"><key1>Test Error 1 Test Error 2</key1>" +
                    "<key2>Test Error 3</key2></Error>" :
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<Error><key1>Test Error 1 Test Error 2</key1><key2>Test Error 3</key2></Error>";

            Assert.Equal(expectedContent, res);
        }
    }
}