// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Parser.Html;
using System;
using System.Linq;
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
            var assemblyReferences = new string[] { "System.Abc.dll", "MyApp.ClassLib.dll", };
            var instance = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate,
                "MyApp.Entrypoint",
                "MyNamespace.MyType::MyMethod",
                assemblyReferences,
                Enumerable.Empty<EmbeddedResourceInfo>(),
                linkerEnabled: true);

            // Act & Assert: Start and end is not modified (including formatting)
            Assert.StartsWith(htmlTemplatePrefix, instance);
            Assert.EndsWith(htmlTemplateSuffix, instance);

            // Assert: Boot tag is correct
            var scriptTagText = instance.Substring(htmlTemplatePrefix.Length, instance.Length - htmlTemplatePrefix.Length - htmlTemplateSuffix.Length);
            var parsedHtml = new HtmlParser().Parse("<html><body>" + scriptTagText + "</body></html>");
            var scriptElems = parsedHtml.Body.QuerySelectorAll("script");
            var linkElems = parsedHtml.Body.QuerySelectorAll("link");
            var scriptElem = scriptElems[0];
            Assert.False(scriptElem.HasChildNodes);
            Assert.Equal("_framework/blazor.js", scriptElem.GetAttribute("src"));
            Assert.Equal("MyApp.Entrypoint.dll", scriptElem.GetAttribute("main"));
            Assert.Equal("MyNamespace.MyType::MyMethod", scriptElem.GetAttribute("entrypoint"));
            Assert.Equal("System.Abc.dll,MyApp.ClassLib.dll", scriptElem.GetAttribute("references"));
            Assert.False(scriptElem.HasAttribute("type"));
            Assert.Equal(string.Empty, scriptElem.Attributes["custom1"].Value);
            Assert.Equal("value", scriptElem.Attributes["custom2"].Value);
            Assert.Equal("true", scriptElem.Attributes["linker-enabled"].Value);
        }

        [Fact]
        public void SuppliesHtmlTemplateUnchangedIfNoBootScriptPresent()
        {
            // Arrange
            var htmlTemplate = "<!DOCTYPE html><html><body><h1 style='color:red'>Hello</h1>Some text<script type='irrelevant'>blah</script></body></html>";
            var assemblyReferences = new string[] { "System.Abc.dll", "MyApp.ClassLib.dll" };
            var jsReferences = new string[] { "some/file.js", "another.js" };
            var cssReferences = new string[] { "my/styles.css" };

            var content = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate, "MyApp.Entrypoint", "MyNamespace.MyType::MyMethod", assemblyReferences, Enumerable.Empty<EmbeddedResourceInfo>(), linkerEnabled: true);

            // Assert
            Assert.Equal(htmlTemplate, content);
        }

        [Fact]
        public void InjectsAdditionalTagsForEmbeddedContent()
        {
            // Arrange
            var htmlTemplate = "Start <script id='testboot' type='blazor-boot'></script> End";
            var embeddedContent = new[]
            {
                new EmbeddedResourceInfo(EmbeddedResourceKind.Static, "my/static/file"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.Css, "css/first.css"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.JavaScript, "javascript/first.js"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.Css, "css/second.css"),
                new EmbeddedResourceInfo(EmbeddedResourceKind.JavaScript, "javascript/second.js"),
            };

            // Act
            var resultHtml = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate,
                "MyApp.Entrypoint",
                "MyNamespace.MyType::MyMethod",
                assemblyReferences: new[] { "Something.dll" },
                embeddedContent: embeddedContent,
                linkerEnabled: true);

            // Assert
            var parsedHtml = new HtmlParser().Parse(resultHtml);
            var blazorBootScript = parsedHtml.GetElementById("testboot");
            Assert.NotNull(blazorBootScript);
            Assert.Equal(
                "Start "
                + blazorBootScript.OuterHtml
                 // First we insert the CSS file tags in order
                + Environment.NewLine + "<link rel=\"stylesheet\" href=\"css/first.css\" />"
                + Environment.NewLine + "<link rel=\"stylesheet\" href=\"css/second.css\" />"
                // Then the JS file tags in order, each with 'defer'
                + Environment.NewLine + "<script src=\"javascript/first.js\" defer></script>"
                + Environment.NewLine + "<script src=\"javascript/second.js\" defer></script>"
                + " End",
                resultHtml);
        }
    }
}
