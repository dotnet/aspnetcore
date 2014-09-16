// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class XmlSerializerOutputFormatterTests
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
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal(expectedOutput,
                new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                        .ReadToEnd());
            Assert.True(outputFormatterContext.ActionContext.HttpContext.Response.Body.CanRead);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesSimpleTypes()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter(
                XmlOutputFormatter.GetDefaultXmlWriterSettings());
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                        .ReadToEnd());
            Assert.True(outputFormatterContext.ActionContext.HttpContext.Response.Body.CanRead);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesComplexTypes()
        {
            // Arrange
            var sampleInput = new TestLevelTwo
            {
                SampleString = "TestString",
                TestOne = new TestLevelOne
                {
                    SampleInt = 10,
                    sampleString = "TestLevelOne string"
                }
            };
            var formatter = new XmlSerializerOutputFormatter(
                XmlOutputFormatter.GetDefaultXmlWriterSettings());
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal("<TestLevelTwo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleString>TestString</SampleString>" +
                            "<TestOne><sampleString>TestLevelOne string</sampleString>" +
                            "<SampleInt>10</SampleInt></TestOne></TestLevelTwo>",
                new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                        .ReadToEnd());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesOnModifiedWriterSettings()
        {
            // Arrange
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
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                        new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                                .ReadToEnd());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesUTF16Output()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var outputFormatterContext = 
                GetOutputFormatterContext(sampleInput, sampleInput.GetType(), "application/xml; charset=utf-16");
            var formatter = new XmlSerializerOutputFormatter(
                XmlOutputFormatter.GetDefaultXmlWriterSettings());
            formatter.WriterSettings.OmitXmlDeclaration = false;

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                        new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body,
                                Encodings.UTF16EncodingLittleEndian).ReadToEnd());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterWritesIndentedOutput()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter(
                XmlOutputFormatter.GetDefaultXmlWriterSettings());
            formatter.WriterSettings.Indent = true;
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            var outputString = new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body,
                Encoding.UTF8).ReadToEnd();
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <SampleInt>10</SampleInt>\r\n</DummyClass>",
                outputString);
        }

        [Fact]
        public async Task VerifyBodyIsNotClosedAfterOutputIsWritten()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlSerializerOutputFormatter(
                XmlOutputFormatter.GetDefaultXmlWriterSettings());
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            Assert.True(outputFormatterContext.ActionContext.HttpContext.Response.Body.CanRead);
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

            var response = outputFormatterContext.ActionContext.HttpContext.Response;
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
            if(expectedOutput != null)
            {
                Assert.Equal(expectedOutput, Assert.Single(result).RawValue);
            }
            else
            {
                Assert.Equal(expectedOutput, result);
            }
        }

        private OutputFormatterContext GetOutputFormatterContext(object outputValue, Type outputType,
                                                        string contentType = "application/xml; charset=utf-8")
        {
            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                ActionContext = GetActionContext(contentType)
            };
        }

        private static ActionContext GetActionContext(string contentType)
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.Setup(r => r.ContentType).Returns(contentType);
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.AcceptCharset).Returns(contentType.Split('=')[1]);
            var response = new Mock<HttpResponse>();
            response.SetupGet(f => f.Body).Returns(new MemoryStream());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Response).Returns(response.Object);
            return new ActionContext(httpContext.Object, routeData: null, actionDescriptor: null);
        }
    }
}