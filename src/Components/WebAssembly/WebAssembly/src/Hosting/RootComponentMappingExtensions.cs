// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal static class RootComponentMappingExtensions
    {
        public static void Deconstruct(
            this RootComponentMapping mapping,
            out Type componentType,
            out ParameterView parameters,
            out string selector)
            => (componentType, parameters, selector) = (mapping.ComponentType, mapping.Parameters, mapping.Selector);
    }
}
