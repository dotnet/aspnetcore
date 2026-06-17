// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class HtmlHelperOptionsTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RazorWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task AppWideDefaultsInViewAndPartialView()
    {
        // Arrange
        var expected =
@"<div class=""validation-summary-errors""><validationSummaryElement>MySummary</validationSummaryElement>
<ul><li>A model error occurred.</li>
</ul></div>
<validationMessageElement class=""field-validation-error"">An error occurred.</validationMessageElement>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""text"" value=""2000-01-02T03:04:05.060&#x2B;00:00"" /> </div>

<div class=""validation-summary-errors""><validationSummaryElement>MySummary</validationSummaryElement>
<ul><li>A model error occurred.</li>
</ul></div>
<validationMessageElement class=""field-validation-error"">An error occurred.</validationMessageElement>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""text"" value=""2000-01-02T03:04:05.060&#x2B;00:00"" /> </div>

False";

        // Act
        var body = await Client.GetStringAsync("http://localhost/HtmlHelperOptions/HtmlHelperOptionsDefaultsInView");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    [ReplaceCulture]
    public async Task OverrideAppWideDefaultsInViewAndPartialView()
    {
        // Arrange
        var expected =
@"<div class=""validation-summary-errors""><ValidationSummaryInView>MySummary</ValidationSummaryInView>
<ul><li>A model error occurred.</li>
</ul></div>
<ValidationInView class=""field-validation-error"" data-valmsg-for=""Error"" data-valmsg-replace=""true"">An error occurred.</ValidationInView>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" data-val=""true"" data-val-required=""The MyDate field is required."" id=""MyDate"" name=""MyDate"" type=""text"" value=""02/01/2000 03:04:05 &#x2B;00:00"" /> <ValidationInView class=""field-validation-valid"" data-valmsg-for=""MyDate"" data-valmsg-replace=""true""></ValidationInView></div>

True
<div class=""validation-summary-errors""><ValidationSummaryInPartialView>MySummary</ValidationSummaryInPartialView>
<ul><li>A model error occurred.</li>
</ul></div>
<ValidationInPartialView class=""field-validation-error"" data-valmsg-for=""Error"" data-valmsg-replace=""true"">An error occurred.</ValidationInPartialView>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""text"" value=""02/01/2000 03:04:05 &#x2B;00:00"" /> <ValidationInPartialView class=""field-validation-valid"" data-valmsg-for=""MyDate"" data-valmsg-replace=""true""></ValidationInPartialView></div>

True";

        // Act
        var body = await Client.GetStringAsync("http://localhost/HtmlHelperOptions/OverrideAppWideDefaultsInView");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }
}
