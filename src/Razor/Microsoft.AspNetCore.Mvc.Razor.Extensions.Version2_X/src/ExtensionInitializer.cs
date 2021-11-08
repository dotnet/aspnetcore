// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

internal class ExtensionInitializer : RazorExtensionInitializer
{
    public override void Initialize(RazorProjectEngineBuilder builder)
    {
        RazorExtensions.Register(builder);
    }
}
