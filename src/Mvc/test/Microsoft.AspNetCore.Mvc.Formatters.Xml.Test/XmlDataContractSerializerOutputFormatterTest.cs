// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public class XmlDataContractSerializerOutputFormatterTest
    {
        [DataContract(Name = "DummyClass", Namespace = "")]
        public class DummyClass
        {
            [DataMember]
            public int SampleInt { get; set; }
        }

        [DataContract(Name = "SomeDummyClass", Namespace = "")]
        public class SomeDummyClass : DummyClass
        {
            [DataMember]
            public string SampleString { get; set; }
        }

        [DataContract(Name = "TestLevelOne", Namespace = "")]
        public class TestLevelOne
        {
            [DataMember]
            public int SampleInt { get; set; }
            [DataMember]
            public string sampleString;
        }

        [DataContract(Name = "TestLevelTwo", Namespace = "")]
        public class TestLevelTwo
        {
            [DataMember]
            public string SampleString { get; set; }
            [DataMember]
            public TestLevelOne TestOne { get; set; }
        }

        [DataContract(Name = "Child", Namespace = "")]
        public class Child
        {
            [DataMember]
            public int Id { get; set; }
            [DataMember]
            public Parent Parent { get; set; }
        }

        [DataContract(Name = "Parent", Namespace = "")]
        public class Parent
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public List<Child> Children { get; set; }
        }

        public static IEnumerable<object[]> BasicTypeValues
        {
            get
            {
                yield return new object[] { "sampleString",
                    "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">sampleString</string>" };
                yield return new object[] { 5,
                    "<int xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">5</int>" };
                yield return new object[] { 5.43,
                    "<double xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">5.43</double>" };
                yield return new object[] { 'a',
                    "<char xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">97</char>" };
                yield return new object[] { new DummyClass { SampleInt = 10 },
                    "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<SampleInt>10</SampleInt></DummyClass>" };
                yield return new object[] { new Dictionary<string, string>() { { "Hello", "World" } },
                    "<ArrayOfKeyValueOfstringstring xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                    "xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><KeyValueOfstringstring>" +
                    "<Key>Hello</Key><Value>World</Value></KeyValueOfstringstring></ArrayOfKeyValueOfstringstring>" };
            }
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [MemberData(nameof(BasicTypeValues))]
        public async Task WriteAsync_CanWriteBasicTypes(object input, string expectedOutput)
        {
            // Arrange
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(input, input.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void XmlDataContractSerializer_CachesSerializerForType()
        {
            // Arrange
            var input = new DummyClass { SampleInt = 10 };
            var formatter = new TestXmlDataContractSerializerOutputFormatter();

            var context = GetOutputFormatterContext(input, typeof(DummyClass));
            context.ContentType = new StringSegment("application/xml");

            // Act
            formatter.CanWriteResult(context);
            formatter.CanWriteResult(context);

            // Assert
            Assert.Equal(1, formatter.createSerializerCalledCount);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void DefaultConstructor_ExpectedWriterSettings_Created()
        {
            // Arrange and Act
            var formatter = new XmlDataContractSerializerOutputFormatter();

            // Assert
            var writerSettings = formatter.WriterSettings;
            Assert.NotNull(writerSettings);
            Assert.True(writerSettings.OmitXmlDeclaration);
            Assert.False(writerSettings.CloseOutput);
            Assert.False(writerSettings.CheckCharacters);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task SuppliedWriterSettings_TakeAffect()
        {
            // Arrange
            var writerSettings = FormattingUtilities.GetDefaultXmlWriterSettings();
            writerSettings.OmitXmlDeclaration = false;
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
            var formatter = new XmlDataContractSerializerOutputFormatter(writerSettings);
            var expectedOutput = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                                "<SampleInt>10</SampleInt></DummyClass>";

            // Act
            await formatter.WriteAsync(formatterContext);

            // Assert
            Assert.Same(writerSettings, formatter.WriterSettings);

            var body = formatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesSimpleTypes()
        {
            // Arrange
            var expectedOutput =
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesComplexTypes()
        {
            // Arrange
            var expectedOutput =
                "<TestLevelTwo xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<SampleString>TestString</SampleString>" +
                "<TestOne><SampleInt>10</SampleInt><sampleString>TestLevelOne string</sampleString>" +
                "</TestOne></TestLevelTwo>";

            var sampleInput = new TestLevelTwo
            {
                SampleString = "TestString",
                TestOne = new TestLevelOne
                {
                    SampleInt = 10,
                    sampleString = "TestLevelOne string"
                }
            };
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesOnModifiedWriterSettings()
        {
            // Arrange
            var expectedOutput =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
            var formatter = new XmlDataContractSerializerOutputFormatter(
                new XmlWriterSettings
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

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesUTF16Output()
        {
            // Arrange
            var expectedOutput =
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<SampleInt>10</SampleInt></DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType(),
                "application/xml; charset=utf-16");
            var formatter = new XmlDataContractSerializerOutputFormatter();
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

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesIndentedOutput()
        {
            // Arrange
            var expectedOutput =
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "\r\n  <SampleInt>10</SampleInt>\r\n</DummyClass>";

            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlDataContractSerializerOutputFormatter();
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

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_VerifyBodyIsNotClosedAfterOutputIsWritten()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            Assert.True(body.CanRead);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_DoesntFlushOutputStream()
        {
            // Arrange
            var sampleInput = new DummyClass { SampleInt = 10 };
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            var response = outputFormatterContext.HttpContext.Response;
            response.Body = FlushReportingStream.GetThrowingStream();

            // Act & Assert
            await formatter.WriteAsync(outputFormatterContext);
        }

        public static IEnumerable<object[]> TypesForCanWriteResult
        {
            get
            {
                yield return new object[] { null, typeof(string), true };
                yield return new object[] { null, null, false };
                yield return new object[] { new DummyClass { SampleInt = 5 }, typeof(DummyClass), true };
                yield return new object[] {
                    new Dictionary<string, string> { { "Hello", "world" } }, typeof(object), true };
                yield return new object[] {
                    new Dictionary<string, string> { { "Hello", "world" } }, typeof(Dictionary<string,string>), true };
            }
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [MemberData(nameof(TypesForCanWriteResult))]
        public void CanWriteResult_ReturnsExpectedValueForObjectType(object input, Type declaredType, bool expectedOutput)
        {
            // Arrange
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(input, declaredType);
            outputFormatterContext.ContentType = new StringSegment("application/xml");

            // Act
            var result = formatter.CanWriteResult(outputFormatterContext);

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData("application/xml", false, "application/xml")]
        [InlineData("application/xml", true, "application/xml")]
        [InlineData("application/other", false, null)]
        [InlineData("application/other", true, null)]
        [InlineData("application/*", false, "application/xml")]
        [InlineData("text/*", false, "text/xml")]
        [InlineData("custom/*", false, null)]
        [InlineData("application/xml;v=2", false, null)]
        [InlineData("application/xml;v=2", true, null)]
        [InlineData("application/some.entity+xml", false, null)]
        [InlineData("application/some.entity+xml", true, "application/some.entity+xml")]
        [InlineData("application/some.entity+xml;v=2", true, "application/some.entity+xml;v=2")]
        [InlineData("application/some.entity+other", true, null)]
        public void CanWriteResult_ReturnsExpectedValueForMediaType(
            string mediaType,
            bool isServerDefined,
            string expectedResult)
        {
            // Arrange
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(new object(), typeof(object));
            outputFormatterContext.ContentType = new StringSegment(mediaType);
            outputFormatterContext.ContentTypeIsServerDefined = isServerDefined;

            // Act
            var actualCanWriteValue = formatter.CanWriteResult(outputFormatterContext);

            // Assert
            var expectedContentType = expectedResult ?? mediaType;
            Assert.Equal(expectedResult != null, actualCanWriteValue);
            Assert.Equal(new StringSegment(expectedContentType), outputFormatterContext.ContentType);
        }

        public static IEnumerable<object[]> TypesForGetSupportedContentTypes
        {
            get
            {
                yield return new object[] { typeof(DummyClass), "application/xml" };
                yield return new object[] { typeof(object), "application/xml" };
                yield return new object[] { null, null };
            }
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [MemberData(nameof(TypesForGetSupportedContentTypes))]
        public void GetSupportedContentTypes_ReturnsSupportedTypes(Type type, object expectedOutput)
        {
            // Arrange
            var formatter = new XmlDataContractSerializerOutputFormatter();

            // Act
            var result = formatter.GetSupportedContentTypes(
                "application/xml",
                type);

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

        [Fact]
        public async Task WriteAsync_ThrowsWhenNotConfiguredWithKnownTypes()
        {
            // Arrange
            var sampleInput = new SomeDummyClass { SampleInt = 1, SampleString = "TestString" };
            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, typeof(DummyClass));

            // Act & Assert
            await Assert.ThrowsAsync<SerializationException>(async () => await formatter.WriteAsync(outputFormatterContext));
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_ThrowsWhenNotConfiguredWithPreserveReferences()
        {
            // Arrange
            var child = new Child { Id = 1 };
            var parent = new Parent { Name = "Parent", Children = new List<Child> { child } };
            child.Parent = parent;

            var formatter = new XmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(parent, parent.GetType());

            // Act & Assert
            await Assert.ThrowsAsync<SerializationException>(async () => await formatter.WriteAsync(outputFormatterContext));
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesWhenConfiguredWithRootName()
        {
            // Arrange
            var sampleInt = 10;
            var SubstituteRootName = "SomeOtherClass";
            var SubstituteRootNamespace = "http://tempuri.org";
            var InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            var expectedOutput = string.Format(
                "<{0} xmlns:i=\"{2}\" xmlns=\"{1}\"><SampleInt xmlns=\"\">{3}</SampleInt></{0}>",
                SubstituteRootName,
                SubstituteRootNamespace,
                InstanceNamespace,
                sampleInt);

            var sampleInput = new DummyClass { SampleInt = sampleInt };

            var dictionary = new XmlDictionary();
            var settings = new DataContractSerializerSettings
            {
                RootName = dictionary.Add(SubstituteRootName),
                RootNamespace = dictionary.Add(SubstituteRootNamespace)
            };
            var formatter = new XmlDataContractSerializerOutputFormatter
            {
                SerializerSettings = settings
            };
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesWhenConfiguredWithKnownTypes()
        {
            // Arrange
            var sampleInt = 10;
            var sampleString = "TestString";
            var KnownTypeName = "SomeDummyClass";
            var InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            var expectedOutput = string.Format(
                    "<DummyClass xmlns:i=\"{1}\" xmlns=\"\" i:type=\"{0}\"><SampleInt>{2}</SampleInt>"
                    + "<SampleString>{3}</SampleString></DummyClass>",
                    KnownTypeName,
                    InstanceNamespace,
                    sampleInt,
                    sampleString);

            var sampleInput = new SomeDummyClass
            {
                SampleInt = sampleInt,
                SampleString = sampleString
            };

            var settings = new DataContractSerializerSettings
            {
                KnownTypes = new[] { typeof(SomeDummyClass) }
            };
            var formatter = new XmlDataContractSerializerOutputFormatter
            {
                SerializerSettings = settings
            };
            var outputFormatterContext = GetOutputFormatterContext(sampleInput, typeof(DummyClass));

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task WriteAsync_WritesWhenConfiguredWithPreserveReferences()
        {
            // Arrange
            var sampleId = 1;
            var sampleName = "Parent";
            var InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
            var SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";

            var expectedOutput = string.Format(
                    "<Parent xmlns:i=\"{0}\" z:Id=\"{2}\" xmlns:z=\"{1}\">" +
                    "<Children z:Id=\"2\" z:Size=\"1\">" +
                    "<Child z:Id=\"3\"><Id>{2}</Id><Parent z:Ref=\"1\" i:nil=\"true\" />" +
                    "</Child></Children><Name z:Id=\"4\">{3}</Name></Parent>",
                    InstanceNamespace,
                    SerializationNamespace,
                    sampleId,
                    sampleName);

            var child = new Child { Id = sampleId };
            var parent = new Parent { Name = sampleName, Children = new List<Child> { child } };
            child.Parent = parent;

            var settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true
            };
            var formatter = new XmlDataContractSerializerOutputFormatter
            {
                SerializerSettings = settings
            };
            var outputFormatterContext = GetOutputFormatterContext(parent, parent.GetType());

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            XmlAssert.Equal(expectedOutput, content);
        }

        public static TheoryData<XmlDataContractSerializerOutputFormatter, TestSink> LogsWhenUnableToCreateSerializerForTypeData
        {
            get
            {
                var sink1 = new TestSink();
                var formatter1 = new XmlDataContractSerializerOutputFormatter(new TestLoggerFactory(sink1, enabled: true));

                var sink2 = new TestSink();
                var formatter2 = new XmlDataContractSerializerOutputFormatter(
                    new XmlWriterSettings(),
                    new TestLoggerFactory(sink2, enabled: true));

                return new TheoryData<XmlDataContractSerializerOutputFormatter, TestSink>()
                {
                    { formatter1, sink1 },
                    { formatter2, sink2}
                };
            }
        }

        [Theory]
        [MemberData(nameof(LogsWhenUnableToCreateSerializerForTypeData))]
        public void CannotCreateSerializer_LogsWarning(
            XmlDataContractSerializerOutputFormatter formatter,
            TestSink sink)
        {
            // Arrange
            var outputFormatterContext = GetOutputFormatterContext(new Customer(10), typeof(Customer));

            // Act
            var result = formatter.CanWriteResult(outputFormatterContext);

            // Assert
            Assert.False(result);
            var write = Assert.Single(sink.Writes);
            Assert.Equal(LogLevel.Warning, write.LogLevel);
            Assert.Equal($"An error occurred while trying to create a DataContractSerializer for the type '{typeof(Customer).FullName}'.",
                write.State.ToString());
        }

        [Fact]
        public void DoesNotThrow_OnNoLoggerAnd_WhenUnableToCreateSerializerForType()
        {
            // Arrange
            var formatter = new XmlDataContractSerializerOutputFormatter(); // no logger is being supplied here on purpose
            var outputFormatterContext = GetOutputFormatterContext(new Customer(10), typeof(Customer));

            // Act
            var canWriteResult = formatter.CanWriteResult(outputFormatterContext);

            // Assert
            Assert.False(canWriteResult);
        }

        public static TheoryData<bool, object, string> CanIndentOutputConditionallyData
        {
            get
            {
                var obj = new DummyClass { SampleInt = 10 };
                var newLine = Environment.NewLine;
                return new TheoryData<bool, object, string>()
                {
                    { true, obj, "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    $"{newLine}  <SampleInt>10</SampleInt>{newLine}</DummyClass>" },
                    { false, obj, "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<SampleInt>10</SampleInt></DummyClass>" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CanIndentOutputConditionallyData))]
        public async Task CanIndentOutputConditionally(bool indent, object input, string expectedOutput)
        {
            // Arrange
            var formatter = new IndentingXmlDataContractSerializerOutputFormatter();
            var outputFormatterContext = GetOutputFormatterContext(input, input.GetType());
            outputFormatterContext.HttpContext.Request.QueryString = new QueryString("?indent=" + indent);

            // Act
            await formatter.WriteAsync(outputFormatterContext);

            // Assert
            var body = outputFormatterContext.HttpContext.Response.Body;
            body.Position = 0;

            var content = new StreamReader(body).ReadToEnd();
            Assert.Equal(expectedOutput, content);
        }

        private OutputFormatterWriteContext GetOutputFormatterContext(
            object outputValue,
            Type outputType,
            string contentType = "application/xml; charset=utf-8")
        {
            return new OutputFormatterWriteContext(
                GetHttpContext(contentType),
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                outputType,
                outputValue);
        }

        private static HttpContext GetHttpContext(string contentType)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Headers["Accept-Charset"] = MediaTypeHeaderValue.Parse(contentType).Charset.ToString();
            request.ContentType = contentType;
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

        private class TestXmlDataContractSerializerOutputFormatter : XmlDataContractSerializerOutputFormatter
        {
            public int createSerializerCalledCount = 0;

            protected override DataContractSerializer CreateSerializer(Type type)
            {
                createSerializerCalledCount++;
                return base.CreateSerializer(type);
            }
        }

        private class IndentingXmlDataContractSerializerOutputFormatter : XmlDataContractSerializerOutputFormatter
        {
            public override XmlWriter CreateXmlWriter(
                OutputFormatterWriteContext context,
                TextWriter writer,
                XmlWriterSettings xmlWriterSettings)
            {
                var request = context.HttpContext.Request;
                if (request.Query["indent"] == "True")
                {
                    xmlWriterSettings.Indent = true;
                }

                return base.CreateXmlWriter(context, writer, xmlWriterSettings);
            }
        }
        public class Customer
        {
            public Customer(int id)
            {
            }

            public int MyProperty { get; set; }
        }
    }
}