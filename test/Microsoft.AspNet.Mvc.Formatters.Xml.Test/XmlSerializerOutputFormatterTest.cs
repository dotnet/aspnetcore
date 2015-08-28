// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters.Xml
{
    public class XmlSerializerOutputFormatterTest
    {
        public class DummyClass
        {
            public int SampleInt { get; set; }
        }

        public class TestLevelOne
        {
            public int SampleInt { get; set; }
            public string sampleString;
        }

        public class TestLevelTwo
        {
            public string SampleString { get; set; }
            public TestLevelOne TestOne { get; set; }
        }

        public static IEnumerable<object[]> BasicTypeValues
        {
            get
            {
                yield return new object[] { "sampleString", "<string>sampleString</string>" };
                yield return new object[] { 5, "<int>5</int>" };
                yield return new object[] { 5.43, "<double>5.43</double>" };
                yield return new object[] { 'a', "<char>97</char>" };
                yield return new object[] { new DummyClass { SampleInt = 10 }, "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                    "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>" };
            }
        }

        [Theory]
        [MemberData(nameof(BasicTypeValues))]
        public async Task XmlSerializerOutputFormatterCanWriteBasicTypes(object input, string expectedOutput)
        {
            // Arrange
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(input, typeof(object));

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public void XmlSerializer_CachesSerializerForType()
        {
            // Arrange
            var input = new DummyClass { SampleInt = 10 };
            var formatter = new TestXmlSerializerOutputFormatter();
            var context = GetOutputFormatterContext(input, typeof(DummyClass));

            // Act
            formatter.CanWriteResult(context, MediaTypeHeaderValue.Parse("application/xml"));
            formatter.CanWriteResult(context, MediaTypeHeaderValue.Parse("application/xml"));

            // Assert
            Assert.Equal(1, formatter.createSerializerCalledCount);
        }

        [Fact]
        public void DefaultConstructor_ExpectedWriterSettings_Created()
        {
            // Arrange and Act
            var formatter = new XmlSerializerOutputFormatter();

            // Assert
            var writerSettings = formatter.WriterSettings;
            Assert.NotNull(writerSettings);
            Assert.True(writerSettings.OmitXmlDeclaration);
            Assert.False(writerSettings.CloseOutput);
            Assert.False(writerSettings.CheckCharacters);
        }

        [Fact]
        public async Task SuppliedWriterSettings_TakeAffect()
        {
            // Arrange
            var writerSettings = FormattingUtilities.GetDefaultXmlWriterSettings();
            writerSettings.OmitXmlDeclaration = false;
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
            var formatter = new XmlSerializerOutputFormatter(writerSettings);
            var expectedOutput = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

            // Act
            await formatter.WriteAsync(formatterContext);

            // Assert
            var body = formatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesSimpleTypes()
        {
            // Arrange
            var expectedOutput =
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesComplexTypes()
        {
            // Arrange
            var expectedOutput =
                "<TestLevelTwo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleString>TestString</SampleString>" +
                "<TestOne><sampleString>TestLevelOne string</sampleString>" +
                "<SampleInt>10</SampleInt></TestOne></TestLevelTwo>";

            var sampleInput = new TestLevelTwo
            {
                SampleString = "TestString",
                TestOne = new TestLevelOne
                {
                    SampleInt = 10,
                    sampleString = "TestLevelOne string"
                }
            };
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesOnModifiedWriterSettings()
        {
            // Arrange
            var expectedOutput =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
            var formatter = new XmlSerializerOutputFormatter(
                new System.Xml.XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
                    CloseOutput = false
                });

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesUTF16Output()
        {
            // Arrange
            var expectedOutput =
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var outputFormatterContext =
                GetOutputFormatterContext(sampleInput, sampleInput.GetType(), "application/xml; charset=utf-16");
            var formatter = new XmlSerializerOutputFormatter();
            formatter.WriterSettings.OmitXmlDeclaration = false;

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;
            var content = new StreamReader(
                body,
                new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesIndentedOutput()
        {
            // Arrange
            var expectedOutput =
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <SampleInt>10</SampleInt>\r\n</DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter();
            formatter.WriterSettings.Indent = true;
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [Fact]
        public async Task VerifyBodyIsNotClosedAfterOutputIsWritten()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.HttpContext.Response.Body);
            Assert.True(outputFormatterContext.HttpContext.Response.Body.CanRead);
        }

        public static IEnumerable<object[]> TypesForCanWriteResult
        {
            get
            {
                yield return new object[] { null, typeof(string), true };
                yield return new object[] { null, null, false };
                yield return new object[] { new DummyClass { SampleInt = 5 }, null, true };
                yield return new object[] { new DummyClass { SampleInt = 5 }, typeof(object), true };
                yield return new object[] { null, typeof(object), true };
                yield return new object[] {
                    new Dictionary<string, string> { { "Hello", "world" } }, typeof(object), false };
                yield return new object[] {
                    new Dictionary<string, string> { { "Hello", "world" } }, typeof(Dictionary<string,string>), false };
                yield return new object[] {
                    new[] {"value1", "value2"}, typeof(IEnumerable<string>), true };
                yield return new object[] {
                    Enumerable.Range(1, 2).Select(i => "value" + i).AsQueryable(), typeof(IQueryable<string>), true };
            }
        }

        [Theory]
        [MemberData(nameof(TypesForCanWriteResult))]
        public void XmlSerializer_CanWriteResult(object input, Type declaredType, bool expectedOutput)
        {
            // Arrange
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(input, declaredType);

            // Act
            var result = formatter.CanWriteResult(outputFormatterContext, MediaTypeHeaderValue.Parse("application/xml"));

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterDoesntFlushOutputStream()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            var response = outputFormatterContext.HttpContext.Response;
            response.Body = FlushReportingStream.GetThrowingStream();

            // Act & Assert
            await formatter.WriteAsync(outputFormatterContext);
        }

        public static IEnumerable<object[]> TypesForGetSupportedContentTypes
        {
            get
            {
                yield return new object[] { typeof(DummyClass), typeof(DummyClass), "application/xml" };
                yield return new object[] { typeof(DummyClass), typeof(object), "application/xml" };
                yield return new object[] { null, typeof(DummyClass), "application/xml" };
                yield return new object[] { typeof(DummyClass), null, "application/xml" };
                yield return new object[] { typeof(object), null, "application/xml" };
                yield return new object[] { null, null, null };
            }
        }

        [Theory]
        [MemberData(nameof(TypesForGetSupportedContentTypes))]
        public void XmlSerializer_GetSupportedContentTypes_Returns_SupportedTypes(Type declaredType, Type runtimeType, object expectedOutput)
        {
            // Arrange
            var formatter = new XmlSerializerOutputFormatter();

            // Act
            var result = formatter.GetSupportedContentTypes(
                declaredType, runtimeType, MediaTypeHeaderValue.Parse("application/xml"));

            // Assert
            if (expectedOutput != null)
            {
                Assert.Equal(expectedOutput, Assert.Single(result).ToString());
            }
            else
            {
                Assert.Equal(expectedOutput, result);
            }
        }

        private OutputFormatterContext GetOutputFormatterContext(
            object outputValue,
            Type outputType,
            string contentType = "application/xml; charset=utf-8")
        {
            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                HttpContext = GetHttpContext(contentType)
            };
        }

        private static HttpContext GetHttpContext(string contentType)
        {
            var request = new Mock<HttpRequest>();

            var headers = new HeaderDictionary();
            headers["Accept-Charset"] = MediaTypeHeaderValue.Parse(contentType).Charset;
            request.Setup(r => r.ContentType).Returns(contentType);
            request.SetupGet(r => r.Headers).Returns(headers);

            var response = new Mock<HttpResponse>();
            response.SetupGet(f => f.Body).Returns(new MemoryStream());

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Response).Returns(response.Object);
            return httpContext.Object;
        }

        private class TestXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
        {
            public int createSerializerCalledCount = 0;

            protected override XmlSerializer CreateSerializer(Type type)
            {
                createSerializerCalledCount++;
                return base.CreateSerializer(type);
            }
        }
    }
}