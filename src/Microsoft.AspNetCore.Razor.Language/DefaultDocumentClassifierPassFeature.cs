// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultDocumentClassifierPassFeature : IRazorEngineFeature
    {
        public RazorEngine Engine { get; set; }

        public IList<Action<RazorCodeDocument, ClassDeclarationIRNode>> ConfigureClass { get; } =
            new List<Action<RazorCodeDocument, ClassDeclarationIRNode>>();

        public IList<Action<RazorCodeDocument, NamespaceDeclarationIRNode>> ConfigureNamespace { get; } =
            new List<Action<RazorCodeDocument, NamespaceDeclarationIRNode>>();

        public IList<Action<RazorCodeDocument, RazorMethodDeclarationIRNode>> ConfigureMethod { get; } =
            new List<Action<RazorCodeDocument, RazorMethodDeclarationIRNode>>();
    }
}
