// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    internal class TypeIdentifier
    {
        public string Assembly { get; set; }

        public string Name { get; set; }

        public IDictionary<string, TypeIdentifier> TypeArguments { get; set; }

        internal Type GetTypeOrThrow()
        {
            return Type.GetType($"{Name}, {Assembly}", throwOnError: true);
        }
    }
}
