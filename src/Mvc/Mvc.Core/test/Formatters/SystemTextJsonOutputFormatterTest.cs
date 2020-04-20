// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase
    {
        protected override TextOutputFormatter GetOutputFormatter()
        {
            return SystemTextJsonOutputFormatter.CreateFormatter(new JsonOptions());
        }

        [Fact]
        public async Task WriteResponseBodyAsync_AllowsConfiguringPreserveReferenceHandling()
        {
            // Arrange
            var formatter = GetOutputFormatter();
            ((SystemTextJsonOutputFormatter)formatter).SerializerOptions.ReferenceHandling = ReferenceHandling.Preserve;
            var expectedContent = "{\"$id\":\"1\",\"name\":\"Person\",\"child\":{\"$id\":\"2\",\"name\":\"Child\",\"child\":null,\"parent\":{\"$ref\":\"1\"}},\"parent\":null}";
            var person = new Person
            {
                Name = "Person",
                Child = new Person { Name = "Child", },
            };
            person.Child.Parent = person;

            var mediaType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            var encoding = CreateOrGetSupportedEncoding(formatter, "utf-8", isDefaultEncoding: true);

            var body = new MemoryStream();
            var actionContext = GetActionContext(mediaType, body);

            var outputFormatterContext = new OutputFormatterWriteContext(
                actionContext.HttpContext,
                new TestHttpResponseStreamWriterFactory().CreateWriter,
                typeof(Person),
                person)
            {
                ContentType = new StringSegment(mediaType.ToString()),
            };

            // Act
            await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

            // Assert
            var actualContent = encoding.GetString(body.ToArray());
            Assert.Equal(expectedContent, actualContent);
        }

        private class Person
        {
            public string Name { get; set; }

            public Person Child { get; set; }

            public Person Parent { get; set; }
        }
    }
}
