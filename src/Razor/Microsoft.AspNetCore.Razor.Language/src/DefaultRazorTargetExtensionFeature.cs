// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorTargetExtensionFeature : RazorEngineFeatureBase, IRazorTargetExtensionFeature
{
    public ICollection<ICodeTargetExtension> TargetExtensions { get; } = new List<ICodeTargetExtension>();
}
