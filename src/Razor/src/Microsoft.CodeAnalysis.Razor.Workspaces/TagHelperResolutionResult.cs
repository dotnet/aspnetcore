// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public sealed class TagHelperResolutionResult
    {
        internal static TagHelperResolutionResult Empty = new TagHelperResolutionResult(Array.Empty<TagHelperDescriptor>(), Array.Empty<RazorDiagnostic>());

        public TagHelperResolutionResult(IReadOnlyList<TagHelperDescriptor> descriptors, IReadOnlyList<RazorDiagnostic> diagnostics)
        {
            Descriptors = descriptors;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<TagHelperDescriptor> Descriptors { get; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; }
    }
}