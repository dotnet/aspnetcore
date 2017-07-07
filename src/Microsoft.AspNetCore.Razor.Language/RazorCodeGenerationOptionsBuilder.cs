// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCodeGenerationOptionsBuilder
    {
        public abstract bool DesignTime { get; }

        public abstract int IndentSize { get; set; }

        public abstract bool IndentWithTabs { get; set; }

        public abstract bool SuppressChecksum { get; set; }

        public abstract RazorCodeGenerationOptions Build();
    }
}
