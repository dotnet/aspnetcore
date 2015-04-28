// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class HtmlHelperOptionsTest
    {
        private const string SiteName = nameof(RazorWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task AppWideDefaultsInViewAndPartialView()
        {
            // Arrange
            var expected =
@"<div class=""validation-summary-errors""><validationSummaryElement>MySummary</validationSummaryElement>
<ul><li style=""display:none""></li>
</ul></div>
<validationMessageElement class=""field-validation-error"">An error occurred.</validationMessageElement>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""datetime"" value=""2000-01-02T03:04:05.060&#x2B;00:00"" /> </div>

<div class=""validation-summary-errors""><validationSummaryElement>MySummary</validationSummaryElement>
<ul><li style=""display:none""></li>
</ul></div>
<validationMessageElement class=""field-validation-error"">An error occurred.</validationMessageElement>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""datetime"" value=""2000-01-02T03:04:05.060&#x2B;00:00"" /> </div>

False";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/HtmlHelperOptions/HtmlHelperOptionsDefaultsInView");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task OverrideAppWideDefaultsInViewAndPartialView()
        {
            // Arrange
            var expected =
@"<div class=""validation-summary-errors""><ValidationSummaryInView>MySummary</ValidationSummaryInView>
<ul><li style=""display:none""></li>
</ul></div>
<ValidationInView class=""field-validation-error"" data-valmsg-for=""Error"" data-valmsg-replace=""true"">An error occurred.</ValidationInView>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""datetime"" value=""02/01/2000 03:04:05 &#x2B;00:00"" /> <ValidationInView class=""field-validation-valid"" data-valmsg-for=""MyDate"" data-valmsg-replace=""true""></ValidationInView></div>

True

<div class=""validation-summary-errors""><ValidationSummaryInPartialView>MySummary</ValidationSummaryInPartialView>
<ul><li style=""display:none""></li>
</ul></div>
<ValidationInPartialView class=""field-validation-error"" data-valmsg-for=""Error"" data-valmsg-replace=""true"">An error occurred.</ValidationInPartialView>
<input id=""Prefix!Property1"" name=""Prefix.Property1"" type=""text"" value="""" />
<div class=""editor-label""><label for=""MyDate"">MyDate</label></div>
<div class=""editor-field""><input class=""text-box single-line"" id=""MyDate"" name=""MyDate"" type=""datetime"" value=""02/01/2000 03:04:05 &#x2B;00:00"" /> <ValidationInPartialView class=""field-validation-valid"" data-valmsg-for=""MyDate"" data-valmsg-replace=""true""></ValidationInPartialView></div>

True";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/HtmlHelperOptions/OverrideAppWideDefaultsInView");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}
