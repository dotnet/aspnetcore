// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Parser.Html;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class IndexHtmlWriterTest
    {
        [Fact]
        public void InjectsScriptTagReferencingAssemblyAndDependencies()
        {
            // Arrange
            var htmlTemplatePrefix = @"
                <html>
                <body>
                    <h1>Hello</h1>
                    Some text
                    <script>alert(1)</script>";
            var htmlTemplateSuffix = @"
                </body>
                </html>";
            var htmlTemplate =
                $@"{htmlTemplatePrefix}
                    <script type='blazor-boot' custom1 custom2=""value"">some text that should be removed</script>
                {htmlTemplateSuffix}";
            var dependencies = new string[]
            {
                "System.Abc.dll",
                "MyApp.ClassLib.dll",
            };
            var instance = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate,
                "MyApp.Entrypoint",
                "MyNamespace.MyType::MyMethod", dependencies);

            // Act & Assert: Start and end is not modified (including formatting)
            Assert.StartsWith(htmlTemplatePrefix, instance);
            Assert.EndsWith(htmlTemplateSuffix, instance);

            // Assert: Boot tag is correct
            var scriptTagText = instance.Substring(htmlTemplatePrefix.Length, instance.Length - htmlTemplatePrefix.Length - htmlTemplateSuffix.Length);
            var parsedHtml = new HtmlParser().Parse("<html><body>" + scriptTagText + "</body></html>");
            var scriptElem = parsedHtml.Body.QuerySelector("script");
            Assert.False(scriptElem.HasChildNodes);
            Assert.Equal("_framework/blazor.js", scriptElem.GetAttribute("src"));
            Assert.Equal("MyApp.Entrypoint.dll", scriptElem.GetAttribute("main"));
            Assert.Equal("MyNamespace.MyType::MyMethod", scriptElem.GetAttribute("entrypoint"));
            Assert.Equal("System.Abc.dll,MyApp.ClassLib.dll", scriptElem.GetAttribute("references"));
            Assert.False(scriptElem.HasAttribute("type"));
            Assert.Equal(string.Empty, scriptElem.Attributes["custom1"].Value);
            Assert.Equal("value", scriptElem.Attributes["custom2"].Value);
        }

        [Fact]
        public void SuppliesHtmlTemplateUnchangedIfNoBootScriptPresent()
        {
            // Arrange
            var htmlTemplate = "<!DOCTYPE html><html><body><h1 style='color:red'>Hello</h1>Some text<script type='irrelevant'>blah</script></body></html>";
            var dependencies = new string[]
            {
                "System.Abc.dll",
                "MyApp.ClassLib.dll",
            };

            var content = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate, "MyApp.Entrypoint", "MyNamespace.MyType::MyMethod", dependencies);

            // Assert
            Assert.Equal(htmlTemplate, content);
        }
    }
}
