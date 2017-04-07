// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public interface IRuntimeTargetBuilder
    {
        RazorCodeDocument CodeDocument { get; }

        RazorParserOptions Options { get; }

        ICollection<IRuntimeTargetExtension> TargetExtensions { get; }

        RuntimeTarget Build();
    }
}
