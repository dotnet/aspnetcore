// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ValidationProblemDetails21WrapperTest
    {
        [Fact]
        public void ReadXml_ReadsValidationProblemDetailsXml()
        {
            // Arrange
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ValidationProblemDetails>" +
                "<Title>Some title</Title>" +
                "<Status>400</Status>" +
                "<Instance>Some instance</Instance>" +
                "<key1>Test Value 1</key1>" +
                "<_x005B_key2_x005D_>Test Value 2</_x005B_key2_x005D_>" +
                "<MVC-Errors>" +
                "<error1>Test error 1 Test error 2</error1>" +
                "<_x005B_error2_x005D_>Test error 3</_x005B_error2_x005D_>" +
                "<MVC-Empty>Test error 4</MVC-Empty>" +
                "</MVC-Errors>" +
                "</ValidationProblemDetails>";
            var serializer = new DataContractSerializer(typeof(ValidationProblemDetails21Wrapper));

            // Act
            var value = serializer.ReadObject(
                new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            // Assert
            var problemDetails = Assert.IsType<ValidationProblemDetails21Wrapper>(value).ProblemDetails;
            Assert.Equal("Some title", problemDetails.Title);
            Assert.Equal("Some instance", problemDetails.Instance);
            Assert.Equal(400, problemDetails.Status);

            Assert.Collection(
                problemDetails.Extensions.OrderBy(kvp => kvp.Key),
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

            Assert.Collection(
                problemDetails.Errors.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Empty(kvp.Key);
                    Assert.Equal(new[] { "Test error 4" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("[error2]", kvp.Key);
                    Assert.Equal(new[] { "Test error 3" }, kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("error1", kvp.Key);
                    Assert.Equal(new[] { "Test error 1 Test error 2" }, kvp.Value);
                });
        }

        [Fact]
        public void ReadXml_ReadsValidationProblemDetailsXml_WithNoErrors()
        {
            // Arrange
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ValidationProblemDetails>" +
                "<Title>Some title</Title>" +
                "<Status>400</Status>" +
                "<Instance>Some instance</Instance>" +
                "<key1>Test Value 1</key1>" +
                "<_x005B_key2_x005D_>Test Value 2</_x005B_key2_x005D_>" +
                "</ValidationProblemDetails>";
            var serializer = new DataContractSerializer(typeof(ValidationProblemDetails21Wrapper));

            // Act
            var value = serializer.ReadObject(
                new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            // Assert
            var problemDetails = Assert.IsType<ValidationProblemDetails21Wrapper>(value).ProblemDetails;
            Assert.Equal("Some title", problemDetails.Title);
            Assert.Equal("Some instance", problemDetails.Instance);
            Assert.Equal(400, problemDetails.Status);

            Assert.Collection(
                problemDetails.Extensions,
                kvp =>
                {
                    Assert.Equal("key1", kvp.Key);
                    Assert.Equal("Test Value 1", kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("[key2]", kvp.Key);
                    Assert.Equal("Test Value 2", kvp.Value);
                });

            Assert.Empty(problemDetails.Errors);
        }

        [Fact]
        public void ReadXml_ReadsValidationProblemDetailsXml_WithEmptyErrorsElement()
        {
            // Arrange
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ValidationProblemDetails>" +
                "<Title>Some title</Title>" +
                "<Status>400</Status>" +
                "<MVC-Errors />" +
                "</ValidationProblemDetails>";
            var serializer = new DataContractSerializer(typeof(ValidationProblemDetails21Wrapper));

            // Act
            var value = serializer.ReadObject(
                new MemoryStream(Encoding.UTF8.GetBytes(xml)));

            // Assert
            var problemDetails = Assert.IsType<ValidationProblemDetails21Wrapper>(value).ProblemDetails;
            Assert.Equal("Some title", problemDetails.Title);
            Assert.Equal(400, problemDetails.Status);
            Assert.Empty(problemDetails.Errors);
        }

        [Fact]
        public void WriteXml_WritesValidXml()
        {
            // Arrange
            var problemDetails = new ValidationProblemDetails
            {
                Title = "Some title",
                Detail = "Some detail",
                Extensions =
                {
                    ["key1"] = "Test Value 1",
                    ["[Key2]"] = "Test Value 2"
                },
                Errors =
                {
                    { "error1", new[] {"Test error 1", "Test error 2" } },
                    { "[error2]", new[] {"Test error 3" } },
                    { "", new[] { "Test error 4" } },
                }
            };

            var wrapper = new ValidationProblemDetails21Wrapper(problemDetails);
            var outputStream = new MemoryStream();
            var expectedContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ValidationProblemDetails>" +
                "<Detail>Some detail</Detail>" +
                "<Title>Some title</Title>" +
                "<key1>Test Value 1</key1>" +
                "<_x005B_Key2_x005D_>Test Value 2</_x005B_Key2_x005D_>" +
                "<MVC-Errors>" +
                "<error1>Test error 1 Test error 2</error1>" +
                "<_x005B_error2_x005D_>Test error 3</_x005B_error2_x005D_>" +
                "<MVC-Empty>Test error 4</MVC-Empty>" +
                "</MVC-Errors>" +
                "</ValidationProblemDetails>";

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

        [Fact]
        public void WriteXml_WithNoValidationErrors()
        {
            // Arrange
            var problemDetails = new ValidationProblemDetails
            {
                Title = "Some title",
                Detail = "Some detail",
                Extensions =
                {
                    ["key1"] = "Test Value 1",
                    ["[Key2]"] = "Test Value 2"
                },
            };

            var wrapper = new ValidationProblemDetails21Wrapper(problemDetails);
            var outputStream = new MemoryStream();
            var expectedContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ValidationProblemDetails>" +
                "<Detail>Some detail</Detail>" +
                "<Title>Some title</Title>" +
                "<key1>Test Value 1</key1>" +
                "<_x005B_Key2_x005D_>Test Value 2</_x005B_Key2_x005D_>" +
                "</ValidationProblemDetails>";

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
#pragma warning restore CS0618 // Type or member is obsolete
}
