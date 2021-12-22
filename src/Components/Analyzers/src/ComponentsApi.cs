// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Analyzers;

// Constants for type and method names used in code-generation
// Keep these in sync with the actual definitions
internal static class ComponentsApi
{
    public const string AssemblyName = "Microsoft.AspNetCore.Components";

    public static class ParameterAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.ParameterAttribute";
        public const string MetadataName = FullTypeName;

        public const string CaptureUnmatchedValues = "CaptureUnmatchedValues";
    }

    public static class CascadingParameterAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.CascadingParameterAttribute";
        public const string MetadataName = FullTypeName;
    }

    public static class IComponent
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Components.IComponent";
        public const string MetadataName = FullTypeName;
    }
}
