// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Internal.AspNetCore.Analyzers
{
    internal class PubturnalityDescriptors
    {
        public static DiagnosticDescriptor PUB0001 = new DiagnosticDescriptor(
            "PUB0001",
            "Pubternal type in public API",
            "Pubternal type ('{0}') usage in public API",
            "Usage",
            DiagnosticSeverity.Warning, true);

        public static DiagnosticDescriptor PUB0002 = new DiagnosticDescriptor(
            "PUB0002",
            "Cross assembly pubternal reference",
            "Cross assembly pubternal type ('{0}') reference",
            "Usage",
            DiagnosticSeverity.Error, false);
    }
}
