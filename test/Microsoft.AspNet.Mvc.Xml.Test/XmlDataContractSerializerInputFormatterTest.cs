// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Xml
{
    public class XmlDataContractSerializerInputFormatterTest
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
            public DateTime SampleDate { get; set; }
        }

        [DataContract(Name = "TestLevelTwo", Namespace = "")]
        public class TestLevelTwo
        {
            [DataMember]
            public string SampleString { get; set; }
            [DataMember]
            public TestLevelOne TestOne { get; set; }
        }

        private readonly string requiredErrorMessageFormat = string.Format(
            "{0} does not recognize '{1}', so instead use '{2}' with '{3}' set to '{4}' for value type property " +
            "'{{0}}' on type '{{1}}'.",
            typeof(DataContractSerializer).FullName,
            typeof(RequiredAttribute).FullName,
            typeof(DataMemberAttribute).FullName,
            nameof(DataMemberAttribute.IsRequired),
            bool.TrueString);

        [Theory]
        [InlineData("application/xml", true)]
        [InlineData("application/*", true)]
        [InlineData("*/*", true)]
        [InlineData("text/xml", true)]
        [InlineData("text/*", true)]
        [InlineData("text/json", false)]
        [InlineData("application/json", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes("content");

            var actionContext = GetActionContext(contentBytes, contentType: requestContentType);
            var formatterContext = new InputFormatterContext(actionContext, typeof(string));

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Fact]
        public void XmlDataContractSerializer_CachesSerializerForType()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass><SampleInt>10</SampleInt></DummyClass>";
            var formatter = new TestXmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act
            formatter.CanRead(context);
            formatter.CanRead(context);

            // Assert
            Assert.Equal(1, formatter.createSerializerCalledCount);
        }

        [Fact]
        public void HasProperSuppportedMediaTypes()
        {
            // Arrange & Act
            var formatter = new XmlDataContractSerializerInputFormatter();

            // Assert
            Assert.True(formatter.SupportedMediaTypes
                                 .Select(content => content.ToString())
                                 .Contains("application/xml"));
            Assert.True(formatter.SupportedMediaTypes
                                 .Select(content => content.ToString())
                                 .Contains("text/xml"));
        }

        [Fact]
        public void HasProperSuppportedEncodings()
        {
            // Arrange & Act
            var formatter = new XmlDataContractSerializerInputFormatter();

            // Assert
            Assert.True(formatter.SupportedEncodings.Any(i => i.WebName == "utf-8"));
            Assert.True(formatter.SupportedEncodings.Any(i => i.WebName == "utf-16"));
        }

        [Fact]
        public async Task ReadAsync_ReadsSimpleTypes()
        {
            // Arrange
            var expectedInt = 10;
            var expectedString = "TestString";

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                                "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
                                "<sampleString>" + expectedString + "</sampleString></TestLevelOne>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelOne));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            Assert.IsType<TestLevelOne>(model);

            var levelOneModel = model as TestLevelOne;
            Assert.Equal(expectedInt, levelOneModel.SampleInt);
            Assert.Equal(expectedString, levelOneModel.sampleString);
        }

        [Fact]
        public async Task ReadAsync_ReadsComplexTypes()
        {
            // Arrange
            var expectedInt = 10;
            var expectedString = "TestString";
            var expectedLevelTwoString = "102";

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<TestLevelTwo><SampleString>" + expectedLevelTwoString + "</SampleString>" +
                        "<TestOne><SampleInt>" + expectedInt + "</SampleInt>" +
                        "<sampleString>" + expectedString + "</sampleString></TestOne></TestLevelTwo>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            Assert.IsType<TestLevelTwo>(model);

            var levelTwoModel = model as TestLevelTwo;
            Assert.Equal(expectedLevelTwoString, levelTwoModel.SampleString);
            Assert.Equal(expectedInt, levelTwoModel.TestOne.SampleInt);
            Assert.Equal(expectedString, levelTwoModel.TestOne.sampleString);
        }

        [Fact]
        public async Task ReadAsync_ReadsWhenMaxDepthIsModified()
        {
            // Arrange
            var expectedInt = 10;

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass><SampleInt>" + expectedInt + "</SampleInt></DummyClass>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            formatter.MaxDepth = 10;
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));


            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            Assert.IsType<DummyClass>(model);
            var dummyModel = model as DummyClass;
            Assert.Equal(expectedInt, dummyModel.SampleInt);
        }

        [Fact]
        public async Task ReadAsync_ThrowsOnExceededMaxDepth()
        {
            if (TestPlatformHelper.IsMono)
            {
                // ReaderQuotas are not honored on Mono
                return;
            }

            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<TestLevelTwo><SampleString>test</SampleString>" +
                        "<TestOne><SampleInt>10</SampleInt>" +
                        "<sampleString>test</sampleString></TestOne></TestLevelTwo>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            formatter.MaxDepth = 1;
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

            // Act & Assert
            await Assert.ThrowsAsync(typeof(SerializationException), async () => await formatter.ReadAsync(context));
        }

        [Fact]
        public async Task ReadAsync_ThrowsWhenReaderQuotasAreChanged()
        {
            if (TestPlatformHelper.IsMono)
            {
                // ReaderQuotas are not honored on Mono
                return;
            }

            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<TestLevelTwo><SampleString>test</SampleString>" +
                        "<TestOne><SampleInt>10</SampleInt>" +
                        "<sampleString>test</sampleString></TestOne></TestLevelTwo>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            formatter.XmlDictionaryReaderQuotas.MaxStringContentLength = 2;
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

            // Act & Assert
            await Assert.ThrowsAsync(typeof(SerializationException), async () => await formatter.ReadAsync(context));
        }

        [Fact]
        public void SetMaxDepth_ThrowsWhenMaxDepthIsBelowOne()
        {
            // Arrange
            var formatter = new XmlDataContractSerializerInputFormatter();

            // Act & Assert
            Assert.Throws(typeof(ArgumentException), () => formatter.MaxDepth = 0);
        }

        [Fact]
        public async Task ReadAsync_VerifyStreamIsOpenAfterRead()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass><SampleInt>10</SampleInt></DummyClass>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            Assert.True(context.ActionContext.HttpContext.Request.Body.CanRead);
        }

        [Fact]
        public async Task ReadAsync_ThrowsOnInvalidCharacters()
        {
            // Arrange
            var expectedException = TestPlatformHelper.IsMono ? typeof(SerializationException) :
                                                                typeof(XmlException);
            var expectedMessage = TestPlatformHelper.IsMono ?
                "Expected element 'TestLevelTwo' in namespace '', but found Element node 'DummyClass' in namespace ''" :
                "The encoding in the declaration 'UTF-8' does not match the encoding of the document 'utf-16LE'.";
            var inpStart = Encodings.UTF16EncodingLittleEndian.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass><SampleInt>");
            byte[] inp = { 192, 193 };
            var inpEnd = Encodings.UTF16EncodingLittleEndian.GetBytes("</SampleInt></DummyClass>");

            var contentBytes = new byte[inpStart.Length + inp.Length + inpEnd.Length];
            Buffer.BlockCopy(inpStart, 0, contentBytes, 0, inpStart.Length);
            Buffer.BlockCopy(inp, 0, contentBytes, inpStart.Length, inp.Length);
            Buffer.BlockCopy(inpEnd, 0, contentBytes, inpStart.Length + inp.Length, inpEnd.Length);

            var formatter = new XmlDataContractSerializerInputFormatter();
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

            // Act
            var ex = await Assert.ThrowsAsync(expectedException, () => formatter.ReadAsync(context));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task ReadAsync_IgnoresBOMCharacters()
        {
            // Arrange
            var sampleString = "Test";
            var sampleStringBytes = Encoding.UTF8.GetBytes(sampleString);
            var inputStart = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
                "<TestLevelTwo><SampleString>" + sampleString);
            byte[] bom = { 0xef, 0xbb, 0xbf };
            var inputEnd = Encoding.UTF8.GetBytes("</SampleString></TestLevelTwo>");
            var expectedBytes = new byte[sampleString.Length + bom.Length];

            var contentBytes = new byte[inputStart.Length + bom.Length + inputEnd.Length];
            Buffer.BlockCopy(inputStart, 0, contentBytes, 0, inputStart.Length);
            Buffer.BlockCopy(bom, 0, contentBytes, inputStart.Length, bom.Length);
            Buffer.BlockCopy(inputEnd, 0, contentBytes, inputStart.Length + bom.Length, inputEnd.Length);

            var formatter = new XmlDataContractSerializerInputFormatter();
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            var levelTwoModel = model as TestLevelTwo;
            Buffer.BlockCopy(sampleStringBytes, 0, expectedBytes, 0, sampleStringBytes.Length);
            Buffer.BlockCopy(bom, 0, expectedBytes, sampleStringBytes.Length, bom.Length);
            Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(levelTwoModel.SampleString));
        }

        [Fact]
        public async Task ReadAsync_AcceptsUTF16Characters()
        {
            // Arrange
            var expectedInt = 10;
            var expectedString = "TestString";

            var input = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" +
                                "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
                                "<sampleString>" + expectedString + "</sampleString></TestLevelOne>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF16EncodingLittleEndian.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(TestLevelOne));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            Assert.IsType<TestLevelOne>(model);

            var levelOneModel = model as TestLevelOne;
            Assert.Equal(expectedInt, levelOneModel.SampleInt);
            Assert.Equal(expectedString, levelOneModel.sampleString);
        }

        [Fact]
        public async Task ReadAsync_ThrowsWhenNotConfiguredWithRootName()
        {
            // Arrange
            var SubstituteRootName = "SomeOtherClass";
            var SubstituteRootNamespace = "http://tempuri.org";

            var input = string.Format(
                "<{0} xmlns=\"{1}\"><SampleInt xmlns=\"\">1</SampleInt></{0}>",
                SubstituteRootName,
                SubstituteRootNamespace);
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act & Assert
            await Assert.ThrowsAsync(typeof(SerializationException), async () => await formatter.ReadAsync(context));
        }

        [Fact]
        public async Task ReadAsync_ReadsWhenConfiguredWithRootName()
        {
            // Arrange
            var expectedInt = 10;
            var SubstituteRootName = "SomeOtherClass";
            var SubstituteRootNamespace = "http://tempuri.org";

            var input = string.Format(
                "<{0} xmlns=\"{1}\"><SampleInt xmlns=\"\">{2}</SampleInt></{0}>",
                SubstituteRootName,
                SubstituteRootNamespace,
                expectedInt);

            var dictionary = new XmlDictionary();
            var settings = new DataContractSerializerSettings
            {
                RootName = dictionary.Add(SubstituteRootName),
                RootNamespace = dictionary.Add(SubstituteRootNamespace)
            };
            var formatter = new XmlDataContractSerializerInputFormatter
            {
                SerializerSettings = settings
            };
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            var dummyModel = Assert.IsType<DummyClass>(model);
            Assert.Equal(expectedInt, dummyModel.SampleInt);
        }

        [Fact]
        public async Task ReadAsync_ThrowsWhenNotConfiguredWithKnownTypes()
        {
            // Arrange
            var KnownTypeName = "SomeDummyClass";
            var InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            var input = string.Format(
                    "<DummyClass i:type=\"{0}\" xmlns:i=\"{1}\"><SampleInt>1</SampleInt>"
                    + "<SampleString>Some text</SampleString></DummyClass>",
                    KnownTypeName,
                    InstanceNamespace);
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act & Assert
            await Assert.ThrowsAsync(typeof(SerializationException), async () => await formatter.ReadAsync(context));
        }

        [Fact]
        public async Task ReadAsync_ReadsWhenConfiguredWithKnownTypes()
        {
            // Arrange
            var expectedInt = 10;
            var expectedString = "TestString";
            var KnownTypeName = "SomeDummyClass";
            var InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            var input = string.Format(
                    "<DummyClass i:type=\"{0}\" xmlns:i=\"{1}\"><SampleInt>{2}</SampleInt>"
                    + "<SampleString>{3}</SampleString></DummyClass>",
                    KnownTypeName,
                    InstanceNamespace,
                    expectedInt,
                    expectedString);
            var settings = new DataContractSerializerSettings
            {
                KnownTypes = new[] { typeof(SomeDummyClass) }
            };
            var formatter = new XmlDataContractSerializerInputFormatter
            {
                SerializerSettings = settings
            };
            var contentBytes = Encoding.UTF8.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.NotNull(model);
            var dummyModel = Assert.IsType<SomeDummyClass>(model);
            Assert.Equal(expectedInt, dummyModel.SampleInt);
            Assert.Equal(expectedString, dummyModel.SampleString);
        }

        [Fact]
        public async Task PostingListOfModels_HasRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ArrayOfAddress " +
                        "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\" " +
                        "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                        "<Address><IsResidential>true</IsResidential><Zipcode>98052" +
                        "</Zipcode></Address></ArrayOfAddress>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(List<Address>));

            // Act
            var model = await formatter.ReadAsync(context) as List<Address>;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(1, model.Count);
            Assert.Equal(98052, model[0].Zipcode);
            Assert.Equal(true, model[0].IsResidential);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                        string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                        string.Format(
                            requiredErrorMessageFormat, 
                            nameof(Address.IsResidential), 
                            typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModel_HasRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<Address xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><IsResidential>" +
                        "true</IsResidential><Zipcode>98052</Zipcode></Address>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(Address));

            // Act
            var model = await formatter.ReadAsync(context) as Address;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(98052, model.Zipcode);
            Assert.Equal(true, model.IsResidential);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                        string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                        string.Format(
                            requiredErrorMessageFormat, 
                            nameof(Address.IsResidential), 
                            typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModelWithProperty_HasRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ModelWithPropertyHavingRequiredAttributeValidationErrors " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><AddressProperty>" +
                "<IsResidential>true</IsResidential><Zipcode>98052</Zipcode></AddressProperty>" +
                "</ModelWithPropertyHavingRequiredAttributeValidationErrors>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(
                contentBytes,
                typeof(ModelWithPropertyHavingRequiredAttributeValidationErrors));

            // Act
            var model = await formatter.ReadAsync(context) as ModelWithPropertyHavingRequiredAttributeValidationErrors;

            // Assert
            Assert.NotNull(model);
            Assert.NotNull(model.AddressProperty);
            Assert.Equal(98052, model.AddressProperty.Zipcode);
            Assert.Equal(true, model.AddressProperty.IsResidential);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModel_WithCollectionProperty_HasRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ModelWithCollectionPropertyHavingRequiredAttributeValidationErrors" +
                " xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Addresses><Address>" +
                "<IsResidential>true</IsResidential><Zipcode>98052</Zipcode></Address></Addresses>" +
                "</ModelWithCollectionPropertyHavingRequiredAttributeValidationErrors>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(
                contentBytes,
                typeof(ModelWithCollectionPropertyHavingRequiredAttributeValidationErrors));

            // Act
            var model = await formatter.ReadAsync(context)
                as ModelWithCollectionPropertyHavingRequiredAttributeValidationErrors;

            // Assert
            Assert.NotNull(model);
            Assert.NotNull(model.Addresses);
            Assert.Equal(98052, model.Addresses[0].Zipcode);
            Assert.Equal(true, model.Addresses[0].IsResidential);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                modelStateKey: typeof(Address).FullName,
                actionContext: context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModelInheritingType_HasRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ModelInheritingTypeHavingRequiredAttributeValidationErrors" +
                " xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<IsResidential>true</IsResidential><Zipcode>98052</Zipcode>" +
                "</ModelInheritingTypeHavingRequiredAttributeValidationErrors>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(
                contentBytes,
                typeof(ModelInheritingTypeHavingRequiredAttributeValidationErrors));

            // Act
            var model = await formatter.ReadAsync(context)
                as ModelInheritingTypeHavingRequiredAttributeValidationErrors;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(98052, model.Zipcode);
            Assert.Equal(true, model.IsResidential);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModelHavingNullableValueTypes_NoRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CarInfo " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                "<ServicedYears xmlns:a=\"http://schemas.datacontract.org/2004/07/System\">" +
                "<a:int>2006</a:int><a:int>2007</a:int></ServicedYears><Year>2005</Year></CarInfo>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(CarInfo));
            var expectedModel = new CarInfo() { Year = 2005, ServicedYears = new List<int?>() };
            expectedModel.ServicedYears.Add(2006);
            expectedModel.ServicedYears.Add(2007);

            // Act
            var model = await formatter.ReadAsync(context) as CarInfo;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(expectedModel.Year, model.Year);
            Assert.Equal(expectedModel.ServicedYears, model.ServicedYears);
            Assert.Empty(context.ActionContext.ModelState);
        }

        [Fact]
        public async Task PostingModel_WithPropertyHavingNullableValueTypes_NoRequiredAttributeValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ModelWithPropertyHavingTypeWithNullableProperties " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\" " +
                "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><CarInfoProperty>" +
                "<ServicedYears xmlns:a=\"http://schemas.datacontract.org/2004/07/System\"><a:int>2006</a:int>" +
                "<a:int>2007</a:int></ServicedYears><Year>2005</Year></CarInfoProperty>" +
                "</ModelWithPropertyHavingTypeWithNullableProperties>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(
                contentBytes,
                typeof(ModelWithPropertyHavingTypeWithNullableProperties));
            var expectedModel = new ModelWithPropertyHavingTypeWithNullableProperties()
            {
                CarInfoProperty = new CarInfo() { Year = 2005, ServicedYears = new List<int?>() }
            };

            expectedModel.CarInfoProperty.ServicedYears.Add(2006);
            expectedModel.CarInfoProperty.ServicedYears.Add(2007);

            // Act
            var model = await formatter.ReadAsync(context) as ModelWithPropertyHavingTypeWithNullableProperties;

            // Assert
            Assert.NotNull(model);
            Assert.NotNull(model.CarInfoProperty);
            Assert.Equal(expectedModel.CarInfoProperty.Year, model.CarInfoProperty.Year);
            Assert.Equal(expectedModel.CarInfoProperty.ServicedYears, model.CarInfoProperty.ServicedYears);
            Assert.Empty(context.ActionContext.ModelState);
        }

        [Fact]
        public async Task PostingModel_WithPropertySelfReferencingItself()
        {
            // Arrange
            var input = "<Employee xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>10</Id><Manager><Id>11</Id><Manager" +
                " i:nil=\"true\"/><Name>Mike</Name></Manager><Name>John</Name></Employee>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(Employee));
            var expectedModel = new Employee()
            {
                Id = 10,
                Name = "John",
                Manager = new Employee()
                {
                    Id = 11,
                    Name = "Mike"
                }
            };

            // Act
            var model = await formatter.ReadAsync(context) as Employee;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(expectedModel.Id, model.Id);
            Assert.Equal(expectedModel.Name, model.Name);
            Assert.NotNull(model.Manager);
            Assert.Equal(expectedModel.Manager.Id, model.Manager.Id);
            Assert.Equal(expectedModel.Manager.Name, model.Manager.Name);
            Assert.Null(model.Manager.Manager);

            Assert.Equal(1, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Employee).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Employee.Id), typeof(Employee).FullName)
                });
        }

        [Fact]
        public async Task PostingModel_WithBothRequiredAndDataMemberRequired_NoValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<Laptop xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>" +
                        "10</Id><SupportsVirtualization>true</SupportsVirtualization></Laptop>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(Laptop));

            // Act
            var model = await formatter.ReadAsync(context) as Laptop;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(10, model.Id);
            Assert.Equal(true, model.SupportsVirtualization);
            Assert.Empty(context.ActionContext.ModelState);
        }

        [Fact]
        public async Task PostingListofModels_WithBothRequiredAndDataMemberRequired_NoValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<ArrayOfLaptop xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Laptop><Id>" +
                        "10</Id><SupportsVirtualization>true</SupportsVirtualization></Laptop></ArrayOfLaptop>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(List<Laptop>));

            // Act
            var model = await formatter.ReadAsync(context) as List<Laptop>;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(1, model.Count);
            Assert.Equal(10, model[0].Id);
            Assert.Equal(true, model[0].SupportsVirtualization);
            Assert.Empty(context.ActionContext.ModelState);
        }

        [Fact]
        public async Task PostingModel_WithRequiredAndDataMemberNoRequired_HasValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<Product xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Id>" +
                        "10</Id><Name>Phone</Name></Product>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(Product));

            // Act
            var model = await formatter.ReadAsync(context) as Product;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(10, model.Id);

            Assert.Equal(2, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Product).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Product.Id), typeof(Product).FullName)
                });

            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingListOfModels_WithRequiredAndDataMemberNoRequired_HasValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<ArrayOfProduct xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Product><Id>" +
                        "10</Id><Name>Phone</Name></Product></ArrayOfProduct>";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(List<Product>));

            // Act
            var model = await formatter.ReadAsync(context) as List<Product>;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(1, model.Count);
            Assert.Equal(10, model[0].Id);
            Assert.Equal("Phone", model[0].Name);

            Assert.Equal(2, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Product).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Product.Id), typeof(Product).FullName)
                });

            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModel_WithDeeperHierarchy_HasValidationErrors()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                        "<Store xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                        " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" i:nil=\"true\" />";
            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(Store));

            // Act
            var model = await formatter.ReadAsync(context) as Store;

            // Assert
            Assert.Null(model);

            Assert.Equal(3, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName)
                });

            AssertModelStateErrorMessages(
                typeof(Employee).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Employee.Id), typeof(Employee).FullName)
                });

            AssertModelStateErrorMessages(
                typeof(Product).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Product.Id), typeof(Product).FullName)
                });
        }

        [Fact]
        public async Task PostingModelOfStructs_WithDeeperHierarchy_HasValidationErrors()
        {
            // Arrange
            var input = "<School i:nil=\"true\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\" " +
                "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(School));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.Null(model);

            Assert.Equal(3, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(School).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(School.Id), typeof(School).FullName)
                });
            AssertModelStateErrorMessages(
                typeof(Website).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Website.Id), typeof(Website).FullName)
                });

            AssertModelStateErrorMessages(
                typeof(Student).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Student.Id), typeof(Student).FullName)
                });
        }
        
        [Fact]
        public async Task PostingModel_WithDictionaryProperty_HasValidationErrorsOnKeyAndValue()
        {
            // Arrange
            var input = "<FavoriteLocations " +
                "i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\"" +
                " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(FavoriteLocations));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.Null(model);

            Assert.Equal(2, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Point).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Point.X), typeof(Point).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Point.Y), typeof(Point).FullName)
                });
            AssertModelStateErrorMessages(
                typeof(Address).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Address.IsResidential), typeof(Address).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Address.Zipcode), typeof(Address).FullName)
                });
        }

        [Fact]
        public async Task PostingModel_WithDifferentValueTypeProperties_HasValidationErrors()
        {
            // Arrange
            var input = "<ValueTypePropertiesModel i:nil=\"true\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.AspNet.Mvc.Xml\" " +
                "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>";

            var formatter = new XmlDataContractSerializerInputFormatter();
            var contentBytes = Encodings.UTF8EncodingWithoutBOM.GetBytes(input);
            var context = GetInputFormatterContext(contentBytes, typeof(ValueTypePropertiesModel));

            // Act
            var model = await formatter.ReadAsync(context);

            // Assert
            Assert.Null(model);

            Assert.Equal(3, context.ActionContext.ModelState.Keys.Count);
            AssertModelStateErrorMessages(
                typeof(Point).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(requiredErrorMessageFormat, nameof(Point.X), typeof(Point).FullName),
                    string.Format(requiredErrorMessageFormat, nameof(Point.X), typeof(Point).FullName)
                });
            AssertModelStateErrorMessages(
                typeof(GpsCoordinate).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(
                        requiredErrorMessageFormat, 
                        nameof(GpsCoordinate.Latitude), 
                        typeof(GpsCoordinate).FullName),
                    string.Format(
                        requiredErrorMessageFormat, 
                        nameof(GpsCoordinate.Longitude), 
                        typeof(GpsCoordinate).FullName)
                });
            AssertModelStateErrorMessages(
                typeof(ValueTypePropertiesModel).FullName,
                context.ActionContext,
                expectedErrorMessages: new[]
                {
                    string.Format(
                        requiredErrorMessageFormat, 
                        nameof(ValueTypePropertiesModel.IntProperty), 
                        typeof(ValueTypePropertiesModel).FullName),
                    string.Format(
                        requiredErrorMessageFormat,
                        nameof(ValueTypePropertiesModel.DateTimeProperty),
                        typeof(ValueTypePropertiesModel).FullName),
                    string.Format(
                        requiredErrorMessageFormat,
                        nameof(ValueTypePropertiesModel.PointProperty),
                        typeof(ValueTypePropertiesModel).FullName),
                    string.Format(
                        requiredErrorMessageFormat,
                        nameof(ValueTypePropertiesModel.GpsCoordinateProperty),
                        typeof(ValueTypePropertiesModel).FullName)
                });
        }

        private void AssertModelStateErrorMessages(
            string modelStateKey,
            ActionContext actionContext,
            IEnumerable<string> expectedErrorMessages)
        {
            ModelState modelState;
            actionContext.ModelState.TryGetValue(modelStateKey, out modelState);

            Assert.NotNull(modelState);
            Assert.NotEmpty(modelState.Errors);

            var actualErrorMessages = modelState.Errors.Select(error =>
            {
                if (string.IsNullOrEmpty(error.ErrorMessage))
                {
                    if (error.Exception != null)
                    {
                        return error.Exception.Message;
                    }
                }

                return error.ErrorMessage;
            });

            Assert.Equal(expectedErrorMessages.Count(), actualErrorMessages.Count());

            if (expectedErrorMessages != null)
            {
                foreach (var expectedErrorMessage in expectedErrorMessages)
                {
                    Assert.Contains(expectedErrorMessage, actualErrorMessages);
                }
            }
        }

        private InputFormatterContext GetInputFormatterContext(byte[] contentBytes, Type modelType)
        {
            var actionContext = GetActionContext(contentBytes);
            var metadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType);
            return new InputFormatterContext(actionContext, metadata.ModelType);
        }

        private static ActionContext GetActionContext(byte[] contentBytes,
                                                      string contentType = "application/xml")
        {
            return new ActionContext(GetHttpContext(contentBytes, contentType),
                                     new AspNet.Routing.RouteData(),
                                     new ActionDescriptor());
        }
        private static HttpContext GetHttpContext(byte[] contentBytes,
                                                  string contentType = "application/xml")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private class TestXmlDataContractSerializerInputFormatter : XmlDataContractSerializerInputFormatter
        {
            public int createSerializerCalledCount = 0;

            protected override DataContractSerializer CreateSerializer(Type type)
            {
                createSerializerCalledCount++;
                return base.CreateSerializer(type);
            }
        }
    }

    public class Address
    {
        [Required]
        public int Zipcode { get; set; }

        [Required]
        public bool IsResidential { get; set; }
    }

    public class CarInfo
    {
        [Required]
        public int? Year { get; set; }

        [Required]
        public List<int?> ServicedYears { get; set; }
    }

    public class Employee
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public Employee Manager { get; set; }
    }

    [DataContract]
    public class Laptop
    {
        [DataMember(IsRequired = true)]
        [Required]
        public int Id { get; set; }

        [DataMember(IsRequired = true)]
        [Required]
        public bool SupportsVirtualization { get; set; }
    }

    [DataContract]
    public class Product
    {
        // Here the property has DataMember but does not set the value 'IsRequired = true'
        [DataMember(Name = "Id")]
        [Required]
        public int Id { get; set; }

        [DataMember(Name = "Name")]
        [Required]
        public string Name { get; set; }

        [DataMember(Name = "Manufacturer")]
        [Required]
        public Manufacturer Manufacturer { get; set; }
    }

    public class ModelWithPropertyHavingRequiredAttributeValidationErrors
    {
        public Address AddressProperty { get; set; }
    }

    public class ModelWithCollectionPropertyHavingRequiredAttributeValidationErrors
    {
        public List<Address> Addresses { get; set; }
    }

    public class ModelInheritingTypeHavingRequiredAttributeValidationErrors : Address
    {
    }

    public class ModelWithPropertyHavingTypeWithNullableProperties
    {
        public CarInfo CarInfoProperty { get; set; }
    }

    public class Store
    {
        public StoreDetails StoreDetails { get; set; }

        public List<Product> Products { get; set; }
    }

    public class StoreDetails
    {
        public List<Employee> Employees { get; set; }

        public Address Address { get; set; }
    }

    public class Manufacturer
    {
        public Address Address { get; set; }
    }

    public struct School
    {
        [Required]
        public int Id { get; set; }

        public List<Student> Students { get; set; }

        public Website Address { get; set; }
    }
    
    public struct Student
    {
        [Required]
        public int Id { get; set; }

        public Website Address { get; set; }
    }

    public struct Website
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }

    public struct ValueTypePropertiesModel
    {
        [Required]
        public int IntProperty { get; set; }

        [Required]
        public int? NullableIntProperty { get; set; }

        [Required]
        public DateTime DateTimeProperty { get; set; }

        [Required]
        public DateTime? NullableDateTimeProperty { get; set; }

        [Required]
        public Point PointProperty { get; set; }

        [Required]
        public Point? NullablePointProperty { get; set; }

        [Required]
        public GpsCoordinate GpsCoordinateProperty { get; set; }

        [Required]
        public GpsCoordinate? NullableGpsCoordinateProperty { get; set; }
    }

    public struct GpsCoordinate
    {
        [Required]
        public Point Latitude { get; set; }

        [Required]
        public Point Longitude { get; set; }
    }

    public struct Point
    {
        [Required]
        public int X { get; set; }

        [Required]
        public int Y { get; set; }
    }

    public class FavoriteLocations
    {
        public Dictionary<Point, Address> Addresses { get; set; }
    }
}