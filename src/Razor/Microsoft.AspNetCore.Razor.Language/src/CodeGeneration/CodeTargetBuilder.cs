// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class CodeTargetBuilder
    {
        public abstract RazorCodeDocument CodeDocument { get; }

        public abstract RazorCodeGenerationOptions Options { get; }

        public abstract ICollection<ICodeTargetExtension> TargetExtensions { get; }

        public abstract CodeTarget Build();
    }
}
