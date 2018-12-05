// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class InjectTargetExtensionTest
    {
        [Fact]
        public void InjectDirectiveTargetExtension_WritesProperty()
        {
            // Arrange
            var context = TestCodeRenderingContext.CreateRuntime();
            var target = new InjectTargetExtension();
            var node = new InjectIntermediateNode()
            {
                TypeName = "PropertyType",
                MemberName = "PropertyName",
            };

            // Act
            target.WriteInjectProperty(context, node);

            // Assert
            Assert.Equal(
                "[global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]" + Environment.NewLine +
                "public PropertyType PropertyName { get; private set; }" + Environment.NewLine,
                context.CodeWriter.GenerateCode());
        }

        [Fact]
        public void InjectDirectiveTargetExtension_WritesPropertyWithLinePragma_WhenSourceIsSet()
        {
            // Arrange
            var context = TestCodeRenderingContext.CreateRuntime();
            var target = new InjectTargetExtension();
            var node = new InjectIntermediateNode()
            {
                TypeName = "PropertyType<ModelType>",
                MemberName = "PropertyName",
                Source = new SourceSpan(
                    filePath: "test-path",
                    absoluteIndex: 0,
                    lineIndex: 1,
                    characterIndex: 1,
                    length: 10)
            };

            // Act
            target.WriteInjectProperty(context, node);

            // Assert
            Assert.Equal(
                "#line 2 \"test-path\"" + Environment.NewLine +
                "[global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]" + Environment.NewLine +
                "public PropertyType<ModelType> PropertyName { get; private set; }" + Environment.NewLine + Environment.NewLine +
                "#line default" + Environment.NewLine +
                "#line hidden" + Environment.NewLine,
                context.CodeWriter.GenerateCode());
        }
    }
}
