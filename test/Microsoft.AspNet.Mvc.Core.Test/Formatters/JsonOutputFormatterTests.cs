// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Routing;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.Formatters
{
    public class JsonOutputFormatterTests
    {
        [Fact]
        public void Creates_SerializerSettings_ByDefault()
        {
            // Arrange
            // Act
            var jsonFormatter = new JsonOutputFormatter();

            // Assert
            Assert.NotNull(jsonFormatter.SerializerSettings);
        }


        [Fact]
        public async Task ChangesTo_DefaultSerializerSettings_TakesEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var jsonFormatter = new JsonOutputFormatter();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            var outputFormatterContext = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;
            Assert.Equal(expectedOutput,
                new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8)
                        .ReadToEnd());
        }

        [Fact]
        public async Task CustomSerializerSettingsObject_TakesEffect()
        {
            // Arrange
            var person = new User() { Name = "John", Age = 35 };
            var expectedOutput = JsonConvert.SerializeObject(person, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });

            var jsonFormatter = new JsonOutputFormatter();
            jsonFormatter.SerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            };

            var outputFormatterContext = GetOutputFormatterContext(person, typeof(User));

            // Act
            await jsonFormatter.WriteResponseBodyAsync(outputFormatterContext);

            // Assert
            Assert.NotNull(outputFormatterContext.ActionContext.HttpContext.Response.Body);
            outputFormatterContext.ActionContext.HttpContext.Response.Body.Position = 0;

            var streamReader = new StreamReader(outputFormatterContext.ActionContext.HttpContext.Response.Body, Encoding.UTF8);
            Assert.Equal(expectedOutput, streamReader.ReadToEnd());
        }

        private OutputFormatterContext GetOutputFormatterContext(object outputValue, Type outputType,
                                            string contentType = "application/xml; charset=utf-8")
        {
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);

            return new OutputFormatterContext
            {
                Object = outputValue,
                DeclaredType = outputType,
                ActionContext = GetActionContext(mediaTypeHeaderValue),
                SelectedEncoding = Encoding.GetEncoding(mediaTypeHeaderValue.Charset)
            };
        }

        private static ActionContext GetActionContext(MediaTypeHeaderValue contentType)
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.Setup(r => r.ContentType).Returns(contentType.RawValue);
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.AcceptCharset).Returns(contentType.Charset);
            var response = new Mock<HttpResponse>();
            response.SetupGet(f => f.Body).Returns(new MemoryStream());
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Response).Returns(response.Object);
            return new ActionContext(httpContext.Object, routeData: null, actionDescriptor: null);
        }

        private sealed class User
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}