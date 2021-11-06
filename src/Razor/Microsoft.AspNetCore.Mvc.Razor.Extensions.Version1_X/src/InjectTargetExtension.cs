// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public class InjectTargetExtension : IInjectTargetExtension
{
    private const string RazorInjectAttribute = "[global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]";

    public void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var property = $"public {node.TypeName} {node.MemberName} {{ get; private set; }}";

        if (node.Source.HasValue)
        {
            using (context.CodeWriter.BuildLinePragma(node.Source.Value, context))
            {
                context.CodeWriter
                    .WriteLine(RazorInjectAttribute)
                    .WriteLine(property);
            }
        }
        else
        {
            context.CodeWriter
                .WriteLine(RazorInjectAttribute)
                .WriteLine(property);
        }
    }
}
