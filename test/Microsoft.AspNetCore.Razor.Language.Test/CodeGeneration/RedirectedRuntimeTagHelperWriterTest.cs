// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RedirectedRuntimeTagHelperWriterTest
    {
        [Fact]
        public void WriteExecuteTagHelpers_Runtime_RendersWithRedirectWriter()
        {
            // Arrange
            var writer = new RedirectedRuntimeTagHelperWriter("test_writer")
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
