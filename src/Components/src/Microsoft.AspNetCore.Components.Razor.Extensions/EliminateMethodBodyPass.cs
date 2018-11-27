// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class EliminateMethodBodyPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run early in the optimization phase
        public override int Order => Int32.MinValue;

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
}
