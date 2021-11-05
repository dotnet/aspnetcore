// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[Flags]
internal enum NodeFlags : byte
{
    None = 0,
    ContainsDiagnostics = 1 << 0,
    ContainsStructuredTrivia = 1 << 1,
    ContainsDirectives = 1 << 2,
    ContainsSkippedText = 1 << 3,
    ContainsAnnotations = 1 << 4,
    IsMissing = 1 << 5,

    InheritMask = ContainsDiagnostics | ContainsStructuredTrivia | ContainsDirectives | ContainsSkippedText | ContainsAnnotations | IsMissing,
}
