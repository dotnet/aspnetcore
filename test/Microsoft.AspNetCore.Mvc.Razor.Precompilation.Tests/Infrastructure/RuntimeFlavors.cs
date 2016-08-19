// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation
{
    public static class RuntimeFlavors
    {
        public static IEnumerable<RuntimeFlavor> SupportedFlavors
        {
            get
            {
                yield return RuntimeFlavor.CoreClr;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    yield return RuntimeFlavor.Clr;
                }
            }
        }
    }
}
