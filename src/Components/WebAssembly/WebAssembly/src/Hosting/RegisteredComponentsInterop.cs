// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
