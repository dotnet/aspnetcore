// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class FilePathComparer
    {
        private static StringComparer _instance;

        public static StringComparer Instance
        {
            get
            {
                if (_instance == null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _instance = StringComparer.Ordinal;
                }
                else if (_instance == null)
                {
                    _instance = StringComparer.OrdinalIgnoreCase;
                }

                return _instance;
            }
        }
    }
}
