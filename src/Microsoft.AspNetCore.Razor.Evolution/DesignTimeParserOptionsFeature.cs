// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DesignTimeParserOptionsFeature : IRazorParserOptionsFeature
    {
        public RazorEngine Engine { get; set; }

        public int Order { get; set; }

        public void Configure(RazorParserOptions options)
        {
            options.DesignTimeMode = true;
        }
    }
}
