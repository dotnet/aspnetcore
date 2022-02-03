// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using AngleSharp.Parser.Html;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class NonNullableReferenceTypesTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>>
{
    public NonNullableReferenceTypesTest(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    private HttpClient Client { get; set; }

    [Fact]
    public async Task CanUseNonNullableReferenceType_WithController_OmitData_ValidationErrors()
    {
        // Arrange
        var parser = new HtmlParser();

        // Act 1
        var response = await Client.GetAsync("http://localhost/NonNullable");

        // Assert 1
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        var document = parser.Parse(content);
        var errors = document.QuerySelectorAll("#errors > ul > li");
        var li = Assert.Single(errors);
        Assert.Empty(li.TextContent);

        var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);
        var formToken = document.RetrieveAntiforgeryToken();

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/NonNullable");
        request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
        request.Content = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("__RequestVerificationToken", formToken),
            });

        // Act 2
        response = await Client.SendAsync(request);

        // Assert 2
        //
        // OK means there were validation errors.
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        content = await response.Content.ReadAsStringAsync();

        document = parser.Parse(content);
        errors = errors = document.QuerySelectorAll("#errors > ul > li");
        Assert.Equal(2, errors.Length); // Not validating BCL error messages
    }

    [Fact]
    public async Task CanUseNonNullableReferenceType_WithController_SubmitData_NoError()
    {
        // Arrange
        var parser = new HtmlParser();

        // Act 1
        var response = await Client.GetAsync("http://localhost/NonNullable");

        // Assert 1
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        var document = parser.Parse(content);
        var errors = document.QuerySelectorAll("#errors > ul > li");
        var li = Assert.Single(errors);
        Assert.Empty(li.TextContent);

        var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);
        var formToken = document.RetrieveAntiforgeryToken();

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/NonNullable");
        request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
        request.Content = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string, string>("Name", "Pranav"),
                new KeyValuePair<string, string>("description", "Meme")
            });

        // Act 2
        response = await Client.SendAsync(request);

        // Assert 2
        //
        // Redirect means there were no validation errors.
        await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task CanUseNonNullableReferenceType_WithController_DefaultValueParameter_NoError()
    {
        // Act 1
        var response = await Client.GetAsync("http://localhost/api/NonNullable");

        // Assert 1
        _ = await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Assert 2
        Assert.NotNull(content);
    }
}
