// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal class RegisteredComponentsInterop
    {
        private static readonly string Prefix = "Blazor._internal.registeredComponents.";

        public static readonly string GetRegisteredComponentsCount = Prefix + "getRegisteredComponentsCount";

        public static readonly string GetId = Prefix + "getId";

        public static readonly string GetAssembly = Prefix + "getAssembly";

        public static readonly string GetTypeName = Prefix + "getTypeName";

        public static readonly string GetParameterDefinitions = Prefix + "getParameterDefinitions";

        public static readonly string GetParameterValues = Prefix + "getParameterValues";
    }
}
