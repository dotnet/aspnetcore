// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Analyzers
{
    // Constants for type and method names used in code-generation
    // Keep these in sync with the actual definitions
    internal static class ComponentsApi
    {
        public static readonly string AssemblyName = "Microsoft.AspNetCore.Components";

        public static class ParameterAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.ParameterAttribute";
            public static readonly string MetadataName = FullTypeName;

            public static readonly string CaptureUnmatchedValues = "CaptureUnmatchedValues";
        }

        public static class CascadingParameterAttribute
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.CascadingParameterAttribute";
            public static readonly string MetadataName = FullTypeName;
        }

        public static class IComponent
        {
            public static readonly string FullTypeName = "Microsoft.AspNetCore.Components.IComponent";
            public static readonly string MetadataName = FullTypeName;
        }
    }
}
