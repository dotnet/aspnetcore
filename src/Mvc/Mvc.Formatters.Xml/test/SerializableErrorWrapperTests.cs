// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

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
        var serializableError = new SerializableError
            {
                { "key1", "key1-error" }
            };

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
            "<Error><MVC-Empty>Test error 0</MVC-Empty>" +
            "<key1>Test Error 1 Test Error 2</key1>" +
            "<key2>Test Error 3</key2>" +
            "<list_x005B_3_x005D_.key3>Test Error 4</list_x005B_3_x005D_.key3></Error>";
        var serializer = new DataContractSerializer(typeof(SerializableErrorWrapper));

        // Act
        var wrapper = (SerializableErrorWrapper)serializer.ReadObject(
            new MemoryStream(Encoding.UTF8.GetBytes(serializableErrorXml)));
        var errors = wrapper.SerializableError;

        // Assert
        Assert.Collection(
            errors,
            kvp =>
            {
                Assert.Equal(string.Empty, kvp.Key);
                Assert.Equal("Test error 0", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("key1", kvp.Key);
                Assert.Equal("Test Error 1 Test Error 2", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("key2", kvp.Key);
                Assert.Equal("Test Error 3", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("list[3].key3", kvp.Key);
                Assert.Equal("Test Error 4", kvp.Value);
            });
    }

    [Fact]
    public void WriteXml_WritesValidXml()
    {
        // Arrange
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "Test error 0");
        modelState.AddModelError("key1", "Test Error 1");
        modelState.AddModelError("key1", "Test Error 2");
        modelState.AddModelError("key2", "Test Error 3");
        modelState.AddModelError("list[3].key3", "Test Error 4");
        var serializableError = new SerializableError(modelState);
        var outputStream = new MemoryStream();
        var expectedContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<Error><MVC-Empty>Test error 0</MVC-Empty>" +
            "<key1>Test Error 1 Test Error 2</key1>" +
            "<key2>Test Error 3</key2>" +
            "<list_x005B_3_x005D_.key3>Test Error 4</list_x005B_3_x005D_.key3></Error>";

        // Act
        using (var xmlWriter = XmlWriter.Create(outputStream))
        {
            var dataContractSerializer = new DataContractSerializer(typeof(SerializableErrorWrapper));
            dataContractSerializer.WriteObject(xmlWriter, new SerializableErrorWrapper(serializableError));
        }
        outputStream.Position = 0;
        var res = new StreamReader(outputStream, Encoding.UTF8).ReadToEnd();

        // Assert
        Assert.Equal(expectedContent, res);
    }
}
