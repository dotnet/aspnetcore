// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public class AddResponseTypeAttributeCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id,
            ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult.Id);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 0)
            {
                return Task.CompletedTask;
            }

            var diagnostic = context.Diagnostics[0];
            if ((diagnostic.Descriptor.Id != ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id) &&
                (diagnostic.Descriptor.Id != ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult.Id))
            {
                return Task.CompletedTask;
            }

            var codeFix = new AddResponseTypeAttributeCodeFixAction(context.Document, diagnostic);

            context.RegisterCodeFix(codeFix, diagnostic);
            return Task.CompletedTask;
        }
    }
}
