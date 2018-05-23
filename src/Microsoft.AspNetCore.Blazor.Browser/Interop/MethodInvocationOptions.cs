// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    internal class MethodInvocationOptions
    {
        public TypeIdentifier Type { get; set; }
        public MethodIdentifier Method { get; set; }

        internal MethodInfo GetMethodOrThrow()
        {
            var type = Type.GetTypeOrThrow();
            var method = Method.GetMethodOrThrow(type);

            return method;
        }
    }
}
