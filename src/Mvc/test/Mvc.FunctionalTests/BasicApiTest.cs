// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class BasicApiTest : IClassFixture<BasicApiFixture>
    {
        private static readonly byte[] PetBytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            .GetBytes(@"{
  ""category"" : {
    ""name"" : ""Cats""
  },
  ""images"": [
    {
        ""url"": ""http://example.com/images/fluffy1.png""
    },
    {
        ""url"": ""http://example.com/images/fluffy2.png""
    },
  ],
  ""tags"": [
    {
        ""name"": ""orange""
    },
    {
        ""name"": ""kitty""
    }
  ],
  ""age"": 2,
  ""hasVaccinations"": ""true"",
  ""name"" : ""fluffy"",
  ""status"" : ""available""
}");

        public BasicApiTest(BasicApiFixture fixture)
        {
            Client = fixture.CreateClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task Token_WithUnknownUser_ReturnsForbidden()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/token?username=fallguy@example.com");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Tests are conditional to avoid occasional CI failures on Windows 8 and Windows 2012 under full framework.
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        public async Task Token_WithKnownUser_ReturnsOkAndToken()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/token?username=reader@example.com");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);

            var token = await response.Content.ReadAsStringAsync();
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task FindByStatus_WithNoToken_ReturnsUnauthorized()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/pet/findByStatus?status=available");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        [InlineData("reader@example.com")]
        [InlineData("writer@example.com")]
        public async Task FindByStatus_WithToken_ReturnsOkAndPet(string username)
        {
            // Arrange & Act 1
            var token = await Client.GetStringAsync($"/token?username={username}");

            // Assert 1 (guard)
            Assert.NotEmpty(token);

            // Arrange 2
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/findByStatus?status=available");
            request.Headers.Add(HeaderNames.Authorization, $"Bearer {token}");

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

            var json = await response.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        public async Task FindById_WithInvalidPetId_ReturnsNotFound()
        {
            // Arrange & Act 1
            var token = await Client.GetStringAsync("/token?username=reader@example.com");

            // Assert 1 (guard)
            Assert.NotEmpty(token);

            // Arrange 2
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/100");
            request.Headers.Add(HeaderNames.Authorization, $"Bearer {token}");

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        public async Task FindById_WithValidPetId_ReturnsOkAndPet()
        {
            // Arrange & Act 1
            var token = await Client.GetStringAsync("/token?username=reader@example.com");

            // Assert 1 (guard)
            Assert.NotEmpty(token);

            // Arrange 2
            var request = new HttpRequestMessage(HttpMethod.Get, "/pet/-1");
            request.Headers.Add(HeaderNames.Authorization, $"Bearer {token}");

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);

            var json = await response.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        public async Task AddPet_WithInsufficientClaims_ReturnsForbidden()
        {
            // Arrange & Act 1
            var token = await Client.GetStringAsync("/token?username=reader@example.com");

            // Assert 1 (guard)
            Assert.NotEmpty(token);

            // Arrange 2
            var request = new HttpRequestMessage(HttpMethod.Post, "/pet")
            {
                Content = new ByteArrayContent(PetBytes)
                {
                    Headers =
                    {
                        { "Content-Type", "application/json" },
                    },
                },
                Headers =
                {
                    { HeaderNames.Authorization, $"Bearer {token}" },
                },
            };

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "See aspnet/Identity#1630")]
        public async Task AddPet_WithValidClaims_ReturnsCreated()
        {
            // Arrange & Act 1
            var token = await Client.GetStringAsync("/token?username=writer@example.com");

            // Assert 1 (guard)
            Assert.NotEmpty(token);

            // Arrange 2
            var request = new HttpRequestMessage(HttpMethod.Post, "/pet")
            {
                Content = new ByteArrayContent(PetBytes)
                {
                    Headers =
                    {
                        { HeaderNames.ContentType, "application/json" },
                    },
                },
                Headers =
                {
                    { HeaderNames.Authorization, $"Bearer {token}" },
                },
            };

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var location = response.Headers.Location.ToString();
            Assert.NotNull(location);
            Assert.EndsWith("/1", location);
        }
    }
}
