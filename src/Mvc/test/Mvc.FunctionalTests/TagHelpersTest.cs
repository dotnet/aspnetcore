// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TagHelpersTest : LoggedTest
{
    // Some tests require comparing the actual response body against an expected response baseline
    // so they require a reference to the assembly on which the resources are located, in order to
    // make the tests less verbose, we get a reference to the assembly with the resources and we
    // use it on all the rest of the tests.
    private static readonly Assembly _resourcesAssembly = typeof(TagHelpersTest).GetTypeInfo().Assembly;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<TagHelpersWebSite.Startup>(LoggerFactory);
        EncodedFactory = new MvcEncodedTestFixture<TagHelpersWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
        EncodedClient = EncodedFactory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<TagHelpersWebSite.Startup> Factory { get; private set; }
    public MvcEncodedTestFixture<TagHelpersWebSite.Startup> EncodedFactory { get; private set; }
    public HttpClient Client { get; private set; }

    public HttpClient EncodedClient { get; private set; }

    [Theory]
    [InlineData("Index")]
    [InlineData("About")]
    [InlineData("Help")]
    [InlineData("UnboundDynamicAttributes")]
    public async Task CanRenderViewsWithTagHelpers(string action)
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/TagHelpersWebSite.Home." + action + ".html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        // The host is not important as everything runs in memory and tests are isolated from each other.
        var response = await Client.GetAsync("http://localhost/Home/" + action);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [ConditionalTheory(Skip = "https://github.com/dotnet/aspnetcore/issues/10423")]
    [InlineData("GlobbingTagHelpers")]
    [InlineData("ViewComponentTagHelpers")]
    public Task CanRenderViewsWithTagHelpersNotReadyForHelix(string action) => CanRenderViewsWithTagHelpers(action);

    [Fact]
    public async Task GivesCorrectCallstackForSyncronousCalls()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/15367
        // Arrange
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await Client.GetAsync("http://localhost/Home/MyHtml"));

        // Assert
        Assert.Equal("Should be visible", exception.InnerException.InnerException.Message);
    }

    [Fact]
    public async Task CanRenderViewsWithTagHelpersAndUnboundDynamicAttributes_Encoded()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/TagHelpersWebSite.Home.UnboundDynamicAttributes.Encoded.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        // The host is not important as everything runs in memory and tests are isolated from each other.
        var response = await EncodedClient.GetAsync("http://localhost/Home/UnboundDynamicAttributes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task ReRegisteringAntiforgeryTokenInsideFormTagHelper_DoesNotAddDuplicateAntiforgeryTokenFields()
    {
        // Arrange
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
        var outputFile = "compiler/resources/TagHelpersWebSite.Employee.DuplicateAntiforgeryTokenRegistration.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("http://localhost/Employee/DuplicateAntiforgeryTokenRegistration");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

        responseContent = responseContent.Trim();

        var forgeryToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
            responseContent, "/Employee/DuplicateAntiforgeryTokenRegistration");
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent, forgeryToken);
    }

    public static TheoryData<string, string> TagHelpersAreInheritedFromViewImportsPagesData
    {
        get
        {
            // action, expected
            return new TheoryData<string, string>
                {
                    {
                        "NestedViewImportsTagHelper",
                        @"<root>root-content</root>


<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithLayoutAndNestedTagHelper",
                        @"layout:<root>root-content</root>
<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithInheritedRemoveTagHelper",
                        @"layout:<root>root-content</root>
page:<root/>
<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithInheritedTagHelperPrefix",
                        @"layout:<root>root-content</root>
page:<root>root-content</root>"
                    },
                    {
                        "ViewWithOverriddenTagHelperPrefix",
                        @"layout:<root>root-content</root>

page:<root>root-content</root>"
                    },
                    {
                        "ViewWithNestedInheritedTagHelperPrefix",
                        @"layout:<root>root-content</root>
page:<root>root-content</root>"
                    },
                    {
                        "ViewWithNestedOverriddenTagHelperPrefix",
                        @"layout:<root>root-content</root>

page:<root>root-content</root>"
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(TagHelpersAreInheritedFromViewImportsPagesData))]
    public async Task TagHelpersAreInheritedFromViewImportsPages(string action, string expected)
    {
        // Arrange & Act
        var result = await Client.GetStringAsync("http://localhost/Home/" + action);

        // Assert
        Assert.Equal(expected, result.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task DefaultInheritedTagsCanBeRemoved()
    {
        // Arrange
        var expected =
@"<a href=""~/VirtualPath"">Virtual path</a>";

        var result = await Client.GetStringAsync("RemoveDefaultInheritedTagHelpers");

        // Assert
        Assert.Equal(expected, result.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task ViewsWithModelMetadataAttributes_CanRenderForm()
    {
        // Arrange
        var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Create.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("http://localhost/Employee/Create");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task ViewsWithModelMetadataAttributes_CanRenderPostedValue()
    {
        // Arrange
        var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Details.AfterCreate.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
        var validPostValues = new Dictionary<string, string>
            {
                { "FullName", "Boo" },
                { "Gender", "M" },
                { "Age", "22" },
                { "EmployeeId", "0" },
                { "JoinDate", "2014-12-01" },
                { "Email", "a@b.com" },
            };
        var postContent = new FormUrlEncodedContent(validPostValues);

        // Act
        var response = await Client.PostAsync("http://localhost/Employee/Create", postContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task ViewsWithModelMetadataAttributes_CanHandleInvalidData()
    {
        // Arrange
        var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Create.Invalid.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
        var validPostValues = new Dictionary<string, string>
            {
                { "FullName", "Boo" },
                { "Gender", "M" },
                { "Age", "1000" },
                { "EmployeeId", "0" },
                { "Email", "a@b.com" },
                { "Salary", "z" },
            };
        var postContent = new FormUrlEncodedContent(validPostValues);

        // Act
        var response = await Client.PostAsync("http://localhost/Employee/Create", postContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Theory]
    [InlineData("Index")]
    [InlineData("CustomEncoder")]
    [InlineData("NullEncoder")]
    [InlineData("TwoEncoders")]
    [InlineData("ThreeEncoders")]
    public async Task EncodersPages_ReturnExpectedContent(string actionName)
    {
        // Arrange
        var outputFile = $"compiler/resources/TagHelpersWebSite.Encoders.{ actionName }.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync($"/Encoders/{ actionName }");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }
}
