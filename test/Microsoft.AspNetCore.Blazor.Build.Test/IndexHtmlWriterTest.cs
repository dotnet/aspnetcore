// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Parser.Html;
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
            var jsReferences = new string[] { "some/file.js", "another.js" };
            var cssReferences = new string[] { "my/styles.css" };
            var instance = IndexHtmlWriter.GetIndexHtmlContents(
                htmlTemplate,
                "MyApp.Entrypoint",
                "MyNamespace.MyType::MyMethod",
                assemblyReferences,
                jsReferences,
                cssReferences,
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

            // Assert: Also contains script tags referencing JS files
            Assert.Equal(
                scriptElems.Skip(1).Select(tag => tag.GetAttribute("src")),
                jsReferences);

            // Assert: Also contains link tags referencing CSS files
            Assert.Equal(
                linkElems.Select(tag => tag.GetAttribute("href")),
                cssReferences);
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
                htmlTemplate, "MyApp.Entrypoint", "MyNamespace.MyType::MyMethod", assemblyReferences, jsReferences, cssReferences, linkerEnabled: true);

            // Assert
            Assert.Equal(htmlTemplate, content);
        }
    }
}
