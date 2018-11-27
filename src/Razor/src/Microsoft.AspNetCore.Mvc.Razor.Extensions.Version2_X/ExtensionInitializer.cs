// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    internal class ExtensionInitializer : RazorExtensionInitializer
    {
        public override void Initialize(RazorProjectEngineBuilder builder)
        {
            RazorExtensions.Register(builder);
        }
    }
}
