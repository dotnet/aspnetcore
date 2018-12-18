// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
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
                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
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
}
