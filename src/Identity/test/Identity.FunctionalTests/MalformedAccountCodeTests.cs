// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using AngleSharp.Html.Dom;
using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class MalformedAccountCodeTests
{
    [Theory]
    [InlineData("V4", "InvalidLength")]
    [InlineData("V4", "TrailingPeriod")]
    [InlineData("V5", "InvalidLength")]
    [InlineData("V5", "TrailingPeriod")]
    public async Task ResetPassword_MalformedCode_ReturnsMissingCodeBadRequest(string version, string malformedCase)
    {
        var emails = new ContosoEmailSender();
        using var factory = CreateFactory(version);
        using var server = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.SetupTestEmailSender(emails)));
        using var registrationClient = server.CreateClient();
        using var resetClient = server.CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";

        await UserStories.RegisterNewUserAsync(registrationClient, email);
        await UserStories.ConfirmEmailAsync(Assert.Single(emails.SentEmails), registrationClient);
        await UserStories.ForgotPasswordAsync(resetClient, email);
        Assert.Equal(2, emails.SentEmails.Count);
        var link = GetMalformedLink(emails.SentEmails[1], malformedCase);

        using var response = await resetClient.GetAsync(link);
        var body = await AssertStatusAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("A code must be supplied for password reset.", body);
    }

    [Theory]
    [InlineData("V4", "InvalidLength")]
    [InlineData("V4", "TrailingPeriod")]
    [InlineData("V5", "InvalidLength")]
    [InlineData("V5", "TrailingPeriod")]
    public async Task ConfirmEmail_MalformedCode_ShowsError(string version, string malformedCase)
    {
        var emails = new ContosoEmailSender();
        using var factory = CreateFactory(version);
        using var server = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.SetupTestEmailSender(emails)));
        using var client = server.CreateClient();
        await UserStories.RegisterNewUserAsync(client);
        var link = GetMalformedLink(Assert.Single(emails.SentEmails), malformedCase);

        using var response = await client.GetAsync(link);
        var body = await AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.Contains("Error confirming your email.", body);
    }

    [Theory]
    [InlineData("V4", "InvalidLength")]
    [InlineData("V4", "TrailingPeriod")]
    [InlineData("V5", "InvalidLength")]
    [InlineData("V5", "TrailingPeriod")]
    public async Task ConfirmEmailChange_MalformedCode_ShowsErrorAndPreservesAccount(string version, string malformedCase)
    {
        var emails = new ContosoEmailSender();
        using var factory = CreateFactory(version);
        using var server = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.SetupTestEmailSender(emails)));
        using var client = server.CreateClient();
        var originalEmail = $"{Guid.NewGuid()}@example.com";
        var newEmail = $"{Guid.NewGuid()}@example.com";
        var index = await UserStories.RegisterNewUserAsync(client, originalEmail);
        await UserStories.SendUpdateEmailAsync(index, newEmail);
        Assert.Equal(2, emails.SentEmails.Count);
        var link = GetMalformedLink(emails.SentEmails[1], malformedCase);

        using var response = await client.GetAsync(link);
        using var scope = server.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(originalEmail);
        Assert.NotNull(user);
        Assert.Equal(originalEmail, user.Email);
        Assert.Equal(originalEmail, user.UserName);
        var body = await AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.Contains("Error changing email.", body);
    }

    [Theory]
    [InlineData("V4", "ResetMissing")]
    [InlineData("V4", "ConfirmMissing")]
    [InlineData("V4", "ChangeMissing")]
    [InlineData("V4", "ConfirmUnknown")]
    [InlineData("V4", "ChangeUnknown")]
    [InlineData("V5", "ResetMissing")]
    [InlineData("V5", "ConfirmMissing")]
    [InlineData("V5", "ChangeMissing")]
    [InlineData("V5", "ConfirmUnknown")]
    [InlineData("V5", "ChangeUnknown")]
    public async Task MissingOrUnknownUser_KeepsExistingResponse(string version, string scenario)
    {
        using var factory = CreateFactory(version);
        using var client = factory.CreateClient();
        var (path, status) = scenario switch
        {
            "ResetMissing" => ("/Identity/Account/ResetPassword", HttpStatusCode.BadRequest),
            "ConfirmMissing" => ("/Identity/Account/ConfirmEmail", HttpStatusCode.Redirect),
            "ChangeMissing" => ("/Identity/Account/ConfirmEmailChange", HttpStatusCode.Redirect),
            "ConfirmUnknown" => ("/Identity/Account/ConfirmEmail?userId=unknown-user&code=a", HttpStatusCode.NotFound),
            "ChangeUnknown" => ("/Identity/Account/ConfirmEmailChange?userId=unknown-user&email=new%40example.com&code=a", HttpStatusCode.NotFound),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        using var response = await client.GetAsync(path);
        var body = await AssertStatusAsync(response, status);
        if (scenario == "ResetMissing")
        {
            Assert.Equal("A code must be supplied for password reset.", body);
        }
        else if (scenario.EndsWith("Unknown", StringComparison.Ordinal))
        {
            Assert.Equal("Unable to load user with ID 'unknown-user'.", body);
        }
        else
        {
            Assert.NotNull(response.Headers.Location);
        }
    }

    private static ServerFactory<ApplicationUserStartup, ApplicationDbContext> CreateFactory(string version)
        => new() { BootstrapFrameworkVersion = version };

    private static string GetMalformedLink(IdentityEmail email, string malformedCase)
    {
        var emailBody = HtmlAssert.IsHtmlFragment(email.Body);
        var anchor = Assert.IsAssignableFrom<IHtmlAnchorElement>(HtmlAssert.HasElement("a", emailBody));
        var uri = new Uri(anchor.Href);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var code = query["code"].ToString();
        Assert.NotEmpty(code);
        var malformedCode = malformedCase switch
        {
            "InvalidLength" => "a",
            "TrailingPeriod" => code + ".",
            _ => throw new ArgumentOutOfRangeException(nameof(malformedCase)),
        };
        var values = query.ToDictionary(pair => pair.Key, pair =>
            string.Equals(pair.Key, "code", StringComparison.OrdinalIgnoreCase) ? malformedCode : pair.Value.ToString());
        return QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), values);
    }

    private static async Task<string> AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        var failure = body.Contains("FormatException", StringComparison.Ordinal) ? " (FormatException in response)" : "";
        Assert.True(response.StatusCode == expected, $"Expected HTTP {(int)expected}; got {(int)response.StatusCode}{failure}.");
        return body;
    }
}
