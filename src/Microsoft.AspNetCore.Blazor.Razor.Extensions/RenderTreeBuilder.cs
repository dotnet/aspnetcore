// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // Constants for method names used in code-generation
    // Keep these in sync with the actual RenderTreeBuilder definitions
    internal static class RenderTreeBuilder
    {
        public static readonly string OpenElement = nameof(OpenElement);

        public static readonly string CloseElement = nameof(CloseElement);

        public static readonly string OpenComponent = nameof(OpenComponent);

        public static readonly string CloseComponent = nameof(CloseElement);

        public static readonly string AddText = nameof(AddText);

        public static readonly string AddAttribute = nameof(AddAttribute);
        
        public static readonly string Clear = nameof(Clear);

        public static readonly string GetFrames = nameof(GetFrames);
    }
}
