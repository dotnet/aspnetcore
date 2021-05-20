// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal class RegisteredComponentsInterop
    {
        private const string Prefix = "Blazor._internal.registeredComponents.";

        public const string GetRegisteredComponentsCount = Prefix + "getRegisteredComponentsCount";

        public const string GetId = Prefix + "getId";

        public const string GetAssembly = Prefix + "getAssembly";

        public const string GetTypeName = Prefix + "getTypeName";

        public const string GetParameterDefinitions = Prefix + "getParameterDefinitions";

        public const string GetParameterValues = Prefix + "getParameterValues";
    }
}
