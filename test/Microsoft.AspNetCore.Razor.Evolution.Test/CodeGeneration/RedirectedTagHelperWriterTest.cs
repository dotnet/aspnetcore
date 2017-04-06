// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class RedirectedTagHelperWriterTest
    {
        // In design time this will not include the 'text writer' parameter.
        [Fact]
        public void WriteExecuteTagHelpers_DesignTime_DoesNormalWrite()
        {
            // Arrange
            var writer = new RedirectedTagHelperWriter(new DesignTimeTagHelperWriter(), "test_writer")
            {
                WriteTagHelperOutputMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
                Writer = new Legacy.CSharpCodeWriter(),
            };

            context.Options.DesignTimeMode = true;

            var node = new ExecuteTagHelpersIRNode();

            // Act
            writer.WriteExecuteTagHelpers(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteExecuteTagHelpers_Runtime_RendersWithRedirectWriter()
        {
            // Arrange
            var writer = new RedirectedTagHelperWriter(new RuntimeTagHelperWriter(), "test_writer")
            {
                WriteTagHelperOutputMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
                Writer = new Legacy.CSharpCodeWriter(),
            };

            var node = new ExecuteTagHelpersIRNode();

            // Act
            writer.WriteExecuteTagHelpers(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
if (!__tagHelperExecutionContext.Output.IsContentModified)
{
    await __tagHelperExecutionContext.SetOutputContentAsync();
}
Test(test_writer, __tagHelperExecutionContext.Output);
__tagHelperExecutionContext = __tagHelperScopeManager.End();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
