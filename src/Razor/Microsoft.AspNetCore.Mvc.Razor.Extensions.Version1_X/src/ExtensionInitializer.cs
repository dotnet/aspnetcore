// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

internal class ExtensionInitializer : RazorExtensionInitializer
{
    public override void Initialize(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Configuration.ConfigurationName == "MVC-1.0")
        {
            RazorExtensions.Register(builder);
        }
        else if (builder.Configuration.ConfigurationName == "MVC-1.1")
        {
            RazorExtensions.Register(builder);
            RazorExtensions.RegisterViewComponentTagHelpers(builder);
        }
    }
}
