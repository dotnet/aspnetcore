// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// This code is temporary. It finds top-level expressions of the form
    ///     @Implements<SomeInterfaceType>()
    /// ... and converts them into interface declarations on the class.
    /// Once we're able to add Blazor-specific directives and have them show up in tooling,
    /// we'll replace this with a simpler and cleaner "@implements SomeInterfaceType" directive.
    /// </summary>
    internal class TemporaryImplementsPass : TemporaryFakeDirectivePass
    {
        // Example: "Implements<MyApp.Namespace.ISomeType<T1, T2>>()"
        // Captures: MyApp.Namespace.ISomeType<T1, T2>
        private const string ImplementsTokenPattern = @"\s*Implements\s*<(.+)\>\s*\(\s*\)\s*";

        public static void Register(IRazorEngineBuilder configuration)
        {
            configuration.Features.Add(new TemporaryImplementsPass());
        }

        private TemporaryImplementsPass() : base(ImplementsTokenPattern)
        {
        }

        protected override void HandleMatchedContent(RazorCodeDocument codeDocument, IEnumerable<string> matchedContent)
        {
            var classNode = codeDocument.GetDocumentIntermediateNode().FindPrimaryClass();
            if (classNode.Interfaces == null)
            {
                classNode.Interfaces = new List<string>();
            }

            foreach (var implementsType in matchedContent)
            {
                classNode.Interfaces.Add(implementsType);
            }
        }
    }
}