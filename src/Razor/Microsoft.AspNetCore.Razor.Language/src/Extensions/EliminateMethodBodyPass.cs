// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class EliminateMethodBodyPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    // Run late in the optimization phase
    public override int Order => int.MaxValue;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (documentNode == null)
        {
            throw new ArgumentNullException(nameof(documentNode));
        }

        var codeGenerationOptions = codeDocument.GetCodeGenerationOptions();
        if (codeGenerationOptions == null || !codeGenerationOptions.SuppressPrimaryMethodBody)
        {
            return;
        }

        var method = documentNode.FindPrimaryMethod();
        if (method == null)
        {
            return;
        }

        method.Children.Clear();

        // After we clear all of the method body there might be some unused fields, which can be
        // blocking if compiling with warnings as errors. Suppress this warning so that it doesn't
        // get annoying in VS.
        documentNode.Children.Insert(documentNode.Children.IndexOf(documentNode.FindPrimaryNamespace()), new CSharpCodeIntermediateNode()
        {
            Children =
                {
                    // Field is assigned but never used
                    new IntermediateToken()
                    {
                        Content = "#pragma warning disable 0414" + Environment.NewLine,
                        Kind = TokenKind.CSharp,
                    },

                    // Field is never assigned
                    new IntermediateToken()
                    {
                        Content = "#pragma warning disable 0649" + Environment.NewLine,
                        Kind = TokenKind.CSharp,
                    },

                    // Field is never used
                    new IntermediateToken()
                    {
                        Content = "#pragma warning disable 0169" + Environment.NewLine,
                        Kind = TokenKind.CSharp,
                    },
                },
        });
    }
}
