// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public static class RazorIRNodeExtensions
    {
        private static readonly IReadOnlyList<RazorDiagnostic> EmptyDiagnostics = Array.Empty<RazorDiagnostic>();

        public static IReadOnlyList<RazorDiagnostic> GetAllDiagnostics(this RazorIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            HashSet<RazorDiagnostic> diagnostics = null;

            AddAllDiagnostics(node);

            return diagnostics?.ToList() ?? EmptyDiagnostics;

            void AddAllDiagnostics(RazorIRNode n)
            {
                if (n.HasDiagnostics)
                {
                    if (diagnostics == null)
                    {
                        diagnostics = new HashSet<RazorDiagnostic>();
                    }

                    diagnostics.UnionWith(n.Diagnostics);
                }

                for (var i = 0; i < n.Children.Count; i++)
                {
                    AddAllDiagnostics(n.Children[i]);
                }
            }
        }
    }
}
