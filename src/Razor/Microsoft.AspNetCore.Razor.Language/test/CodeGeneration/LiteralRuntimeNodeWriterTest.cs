// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class LiteralRuntimeNodeWriterTest
{
    [Fact]
    public void WriteCSharpExpression_UsesWriteLiteral_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = new LiteralRuntimeNodeWriter();
        var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode()
        {
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 3, 0, 3),
        };
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(new IntermediateToken()
        {
            Content = "i++",
            Kind = TokenKind.CSharp,
        });

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GenerateCode();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,4) 13 ""test.cshtml""
WriteLiteral(i++);

#line default
#line hidden
#nullable disable
",
            csharp,
            ignoreLineEndingDifferences: true);
    }
}
