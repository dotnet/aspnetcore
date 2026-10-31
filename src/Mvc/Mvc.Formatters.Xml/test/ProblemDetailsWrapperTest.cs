// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class ProblemDetailsWrapperTest
{
    [Fact]
    public void ReadXml_ReadsProblemDetailsXml()
    {
        // Arrange
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<problem xmlns=\"urn:ietf:rfc:7807\">" +
            "<title>Some title</title>" +
            "<status>403</status>" +
            "<instance>Some instance</instance>" +
            "<key1>Test Value 1</key1>" +
            "<_x005B_key2_x005D_>Test Value 2</_x005B_key2_x005D_>" +
            "<MVC-Empty>Test Value 3</MVC-Empty>" +
            "</problem>";
        var serializer = new DataContractSerializer(typeof(ProblemDetailsWrapper));

        // Act
        var value = serializer.ReadObject(
            new MemoryStream(Encoding.UTF8.GetBytes(xml)));

        // Assert
        var problemDetails = Assert.IsType<ProblemDetailsWrapper>(value).ProblemDetails;
        Assert.Equal("Some title", problemDetails.Title);
        Assert.Equal("Some instance", problemDetails.Instance);
        Assert.Equal(403, problemDetails.Status);

        Assert.Collection(
            problemDetails.Extensions.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal("Test Value 3", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("[key2]", kvp.Key);
                Assert.Equal("Test Value 2", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("key1", kvp.Key);
                Assert.Equal("Test Value 1", kvp.Value);
            });
    }

    [Fact]
    public void WriteXml_WritesValidXml()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Title = "Some title",
            Detail = "Some detail",
            Extensions =
                {
                    ["key1"] = "Test Value 1",
                    ["[Key2]"] = "Test Value 2",
                    [""] = "Test Value 3",
                },
        };

        var wrapper = new ProblemDetailsWrapper(problemDetails);
        var outputStream = new MemoryStream();
        var expectedContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<problem xmlns=\"urn:ietf:rfc:7807\">" +
            "<detail>Some detail</detail>" +
            "<title>Some title</title>" +
            "<key1>Test Value 1</key1>" +
            "<_x005B_Key2_x005D_>Test Value 2</_x005B_Key2_x005D_>" +
            "<MVC-Empty>Test Value 3</MVC-Empty>" +
            "</problem>";

        // Act
        using (var xmlWriter = XmlWriter.Create(outputStream))
        {
            var dataContractSerializer = new DataContractSerializer(wrapper.GetType());
            dataContractSerializer.WriteObject(xmlWriter, wrapper);
        }
        outputStream.Position = 0;
        var res = new StreamReader(outputStream, Encoding.UTF8).ReadToEnd();

        // Assert
        Assert.Equal(expectedContent, res);
    }
}
