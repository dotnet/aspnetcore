// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public interface IRazorTargetExtensionFeature : IRazorEngineFeature
    {
        ICollection<IRuntimeTargetExtension> TargetExtensions { get; }
    }
}
