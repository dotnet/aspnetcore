// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class InputValidationTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<FormatterWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<FormatterWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ValidRequest_IsAccepted()
    {
        // Arrange
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "RequiredProp", "1" },
                { "BindRequiredProp", "2" },
                { "RequiredAndBindRequiredProp", "3" },
                { "requiredParam", "4" },
                { "bindRequiredParam", "5" },
                { "requiredAndBindRequiredParam", "6" },
                { "UnboundRequiredProp", "100" }, // Value should not be used
                { "UnboundBindRequiredProp", "101" }, // Value should not be used
                { "BindNeverRequiredProp", "ignoredValue" }, // Value should not be used
            });

        // Act
        var response = await Client.PostAsync("http://localhost/TopLevelValidation", content);
        var responseText = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("[OptionalProp:0]", responseText);
        Assert.Contains("[RequiredProp:1]", responseText);
        Assert.Contains("[BindRequiredProp:2]", responseText);
        Assert.Contains("[RequiredAndBindRequiredProp:3]", responseText);
        Assert.Contains("[OptionalStringLengthProp:]", responseText);
        Assert.Contains("[OptionalRangeDisplayNameProp:0]", responseText);
        Assert.Contains("[UnboundRequiredProp:0]", responseText);
        Assert.Contains("[UnboundBindRequiredProp:0]", responseText);
        Assert.Contains("[BindNeverRequiredProp:]", responseText);
        Assert.Contains("[optionalParam:0]", responseText);
        Assert.Contains("[requiredParam:4]", responseText);
        Assert.Contains("[bindRequiredParam:5]", responseText);
        Assert.Contains("[requiredAndBindRequiredParam:6]", responseText);
        Assert.Contains("[optionalStringLengthParam:]", responseText);
        Assert.Contains("[optionalRangeDisplayNameParam:0]", responseText);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_IsRejected()
    {
        // Arrange
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "OptionalStringLengthProp", "ThisStringIsTooLongForTheProperty" },
                { "OptionalRangeDisplayNameProp", "123" },
                { "optionalStringLengthParam", "ThisStringIsTooLongForTheParameter" },
                { "optionalRangeDisplayNameParam", "456" },
            });

        // Act
        var response = await Client.PostAsync("http://localhost/TopLevelValidation", content);
        var responseText = await response.Content.ReadAsStringAsync();
        var errors = JsonConvert.DeserializeObject<JObject>(responseText)
            .Properties()
            .ToDictionary(
                prop => prop.Name,
                prop => ((JArray)prop.Value).Single().Value<string>());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10, errors.Count);
        Assert.Equal(
            "The RequiredProp field is required.",
            errors["RequiredProp"]);
        Assert.Equal(
            "A value for the 'BindRequiredProp' parameter or property was not provided.",
            errors["BindRequiredProp"]);
        Assert.Equal(
            "A value for the 'RequiredAndBindRequiredProp' parameter or property was not provided.",
            errors["RequiredAndBindRequiredProp"]);
        Assert.Equal(
            "The field OptionalStringLengthProp must be a string with a maximum length of 5.",
            errors["OptionalStringLengthProp"]);
        Assert.Equal(
            "The field Some Display Name For Prop must be between 1 and 100.",
            errors["OptionalRangeDisplayNameProp"]);
        Assert.Equal(
            "The requiredParam field is required.",
            errors["requiredParam"]);
        Assert.Equal(
            "A value for the 'bindRequiredParam' parameter or property was not provided.",
            errors["bindRequiredParam"]);
        Assert.Equal(
            "A value for the 'requiredAndBindRequiredParam' parameter or property was not provided.",
            errors["requiredAndBindRequiredParam"]);
        Assert.Equal(
            "The field optionalStringLengthParam must be a string with a maximum length of 5.",
            errors["optionalStringLengthParam"]);
        Assert.Equal(
            "The field Some Display Name For Param must be between 1 and 100.",
            errors["optionalRangeDisplayNameParam"]);
    }
}
