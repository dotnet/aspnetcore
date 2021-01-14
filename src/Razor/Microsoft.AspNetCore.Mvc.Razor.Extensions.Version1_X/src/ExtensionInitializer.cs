// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
{
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
}
