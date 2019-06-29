// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using FormatterWebSite;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class AsyncEnumerableTestBase : IClassFixture<MvcTestFixture<StartupWithJsonFormatter>>
    {
        public AsyncEnumerableTestBase(MvcTestFixture<StartupWithJsonFormatter> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public Task AsyncEnumerableReturnedWorks() => AsyncEnumerableWorks();

        [Fact]
        public Task AsyncEnumerableWrappedInTask() => AsyncEnumerableWorks();

        private async Task AsyncEnumerableWorks()
        {
            // Act
            var response = await Client.GetAsync("asyncenumerable/getallprojects");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            // Some sanity tests to verify things serialized correctly.
            var projects = JsonSerializer.Deserialize<List<Project>>(content, TestJsonSerializerOptionsProvider.Options);
            Assert.Equal(10, projects.Count);
            Assert.Equal("Project0", projects[0].Name);
            Assert.Equal("Project9", projects[9].Name);
        }

        [Fact]
        public async Task AsyncEnumerableExceptionsAreThrown()
        {
            // Act
            var response = await Client.GetAsync("asyncenumerable/GetAllProjectsWithError");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.InternalServerError);

            var content = await response.Content.ReadAsStringAsync();

            // Verify that the exception shows up in the callstack
            Assert.Contains(nameof(InvalidTimeZoneException), content);
        }

        [Fact]
        public async Task AsyncEnumerableWithXmlFormatterWorks()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "asyncenumerable/getallprojects");
            request.Headers.Add("Accept", "application/xml");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            // Some sanity tests to verify things serialized correctly.
            var xml = XDocument.Parse(content);
            var @namespace = xml.Root.Name.NamespaceName;
            var projects = xml.Root.Elements(XName.Get("Project", @namespace));
            Assert.Equal(10, projects.Count());

            Assert.Equal("Project0", GetName(projects.ElementAt(0)));
            Assert.Equal("Project9", GetName(projects.ElementAt(9)));

            string GetName(XElement element)
            {
                var name = element.Element(XName.Get("Name", @namespace));
                Assert.NotNull(name);

                return name.Value;
            }
        }
    }
}
