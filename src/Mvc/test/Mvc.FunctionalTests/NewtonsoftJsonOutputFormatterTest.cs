// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using FormatterWebSite.Controllers;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class NewtonsoftJsonOutputFormatterTest : JsonOutputFormatterTestBase<FormatterWebSite.Startup>
{
    [Fact]
    public async Task JsonOutputFormatter_ReturnsIndentedJson()
    {
        // Arrange
        var user = new FormatterWebSite.User()
        {
            Id = 1,
            Alias = "john",
            description = "This is long so we can test large objects " + new string('a', 1024 * 65),
            Designation = "Administrator",
            Name = "John Williams"
        };

        var serializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
        serializerSettings.Formatting = Formatting.Indented;
        var expectedBody = JsonConvert.SerializeObject(user, serializerSettings);

        // Act
        var response = await Client.GetAsync("http://localhost/JsonFormatter/ReturnsIndentedJson");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var actualBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, actualBody);
    }

    [Fact]
    public async Task JsonOutputFormatter_SetsContentLength()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.SimpleModelResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(50, response.Content.Headers.ContentLength);
    }
}
