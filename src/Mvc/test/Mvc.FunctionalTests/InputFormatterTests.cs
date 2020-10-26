// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class InputFormatterTests : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public InputFormatterTests(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task CheckIfXmlInputFormatterIsBeingCalled()
        {
            // Arrange
            var sampleInputInt = 10;
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"><SampleInt>"
                + sampleInputInt.ToString() + "</SampleInt></DummyClass>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await Client.PostAsync("http://localhost/Home/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sampleInputInt.ToString(), await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("utf-8")]
        [InlineData("unicode")]
        public async Task CustomFormatter_IsSelected_ForSupportedContentTypeAndEncoding(string encoding)
        {
            // Arrange
            var content = new StringContent("Test Content", Encoding.GetEncoding(encoding), "text/plain");

            // Act
            var response = await Client.PostAsync("http://localhost/InputFormatter/ReturnInput/", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Test Content", responseBody);
        }

        [Theory]
        [InlineData("image/png")]
        [InlineData("image/jpeg")]
        public async Task CustomFormatter_NotSelected_ForUnsupportedContentType(string contentType)
        {
            // Arrange
            var content = new StringContent("Test Content", Encoding.UTF8, contentType);

            // Act
            var response = await Client.PostAsync("http://localhost/InputFormatter/ReturnInput/", content);

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task BindingWorksForPolymorphicTypes()
        {
            // Act
            var response = await Client.GetAsync("PolymorphicBinding/ModelBound?DerivedProperty=Test");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal("Test", result.DerivedProperty);
        }

        [Fact]
        public async Task ValidationUsesModelMetadataFromActualModelType_ForModelBoundParameters()
        {
            // Act
            var response = await Client.GetAsync("PolymorphicBinding/ModelBound");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Collection(
                result.Properties(),
                p =>
                {
                    Assert.Equal("DerivedProperty", p.Name);
                    var value = Assert.IsType<JArray>(p.Value);
                    Assert.Equal("The DerivedProperty field is required.", value.First);
                });
        }

        [Fact]
        public async Task InputFormatterWorksForPolymorphicTypes()
        {
            // Act
            var input = "Test";
            var response = await Client.PostAsJsonAsync("PolymorphicBinding/InputFormatted", input);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal(input, result.DerivedProperty);
        }

        [Fact]
        public async Task ValidationUsesModelMetadataFromActualModelType_ForInputFormattedParameters()
        {
            // Act
            var response = await Client.PostAsJsonAsync("PolymorphicBinding/InputFormatted", string.Empty);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Collection(
                result.Properties(),
                p =>
                {
                    Assert.Equal("DerivedProperty", p.Name);
                    var value = Assert.IsType<JArray>(p.Value);
                    Assert.Equal("The DerivedProperty field is required.", value.First);
                });
        }

        [Fact]
        public async Task InputFormatterWorksForPolymorphicProperties()
        {
            // Act
            var input = "Test";
            var response = await Client.PostAsJsonAsync("PolymorphicPropertyBinding/Action", input);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal(input, result.DerivedProperty);
        }

        [Fact]
        public async Task ValidationUsesModelMetadataFromActualModelType_ForInputFormattedProperties()
        {
            // Act
            var response = await Client.PostAsJsonAsync("PolymorphicPropertyBinding/Action", string.Empty);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
            var result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Collection(
                result.Properties(),
                p =>
                {
                    Assert.Equal("DerivedProperty", p.Name);
                    var value = Assert.IsType<JArray>(p.Value);
                    Assert.Equal("The DerivedProperty field is required.", value.First);
                });
        }
    }
}
