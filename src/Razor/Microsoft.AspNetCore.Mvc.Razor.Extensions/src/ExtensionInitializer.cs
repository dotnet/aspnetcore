// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal class ExtensionInitializer : RazorExtensionInitializer
{
    public override void Initialize(RazorProjectEngineBuilder builder)
    {
        RazorExtensions.Register(builder);
    }
}
