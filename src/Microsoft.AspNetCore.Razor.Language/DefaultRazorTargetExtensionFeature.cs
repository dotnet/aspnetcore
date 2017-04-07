// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorTargetExtensionFeature : IRazorTargetExtensionFeature
    {
        public RazorEngine Engine { get; set; }

        public ICollection<IRuntimeTargetExtension> TargetExtensions { get; } = new List<IRuntimeTargetExtension>();
    }
}
