// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCodeGenerationOptions
    {
        public static RazorCodeGenerationOptions Create(bool indentWithTabs, int indentSize, bool designTime)
        {
            return new DefaultRazorCodeGenerationOptions(indentWithTabs, indentSize, designTime);
        }

        public static RazorCodeGenerationOptions CreateDefault()
        {
            return new DefaultRazorCodeGenerationOptions(indentWithTabs: false, indentSize: 4, designTime: false);
        }

        public abstract bool DesignTime { get; }

        public abstract bool IndentWithTabs { get; }

        public abstract int IndentSize { get; }
    }
}
