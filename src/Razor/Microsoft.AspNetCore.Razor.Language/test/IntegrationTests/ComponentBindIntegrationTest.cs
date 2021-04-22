// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentBindIntegrationTest : RazorIntegrationTestBase
    {
        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void BindDuplicates_ReportsDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue2"", ""myevent2"")]
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

            // Act
            var result = CompileToCSharp(@"
<div @bind-value=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("RZ9989", diagnostic.Id);
            Assert.Equal(
                "The attribute '@bind-value' was matched by multiple bind attributes. Duplicates:" + Environment.NewLine +
                "Test.BindAttributes" + Environment.NewLine +
                "Test.BindAttributes",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void BindFallback_InvalidSyntax_TooManyParts()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-second-third=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9991", diagnostic.Id);
        }

        [Fact]
        public void BindFallback_InvalidSyntax_TrailingDash()
        {
            // Arrange & Act
            var generated = CompileToCSharp(@"
<input type=""text"" @bind-first-=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("RZ9991", diagnostic.Id);
        }

        [Fact]
        public void Bind_InvalidUseOfDirective_DoesNotThrow()
        {
            // We're looking for VS crash issues. Meaning if the parser returns
            // diagnostics we don't want to throw.
            var generated = CompileToCSharp(@"
@using Microsoft.AspNetCore.Components.Web
<input type=""text"" @bind=""@page"" />
@functions {
    public string page { get; set; } = ""text"";
}", throwOnFailure: false);

            // Assert
            Assert.Collection(
                generated.Diagnostics,
                d => Assert.Equal("RZ2005", d.Id),
                d => Assert.Equal("RZ1011", d.Id));
        }
    }
}
