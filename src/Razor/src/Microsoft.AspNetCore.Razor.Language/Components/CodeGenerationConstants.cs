// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    // Constants for method names used in code-generation
    // Keep these in sync with the actual definitions
    internal static class CodeGenerationConstants
    {
        public static class RazorComponent
        {
            public const string FullTypeName = "Microsoft.AspNetCore.Components.Component";
            public const string BuildRenderTree = "BuildRenderTree";
            public const string BuildRenderTreeParameter = "builder";
        }

        public static class RenderTreeBuilder
        {
            public const string FullTypeName = "Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder";
        }

        public static class InjectDirective
        {
            public const string FullTypeName = "Microsoft.AspNetCore.Razor.Components.InjectAttribute";
        }
    }
}
