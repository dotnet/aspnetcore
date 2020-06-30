// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    internal class ReferenceAssemblyNotFoundException : Exception
    {
        public ReferenceAssemblyNotFoundException(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }
    }
}
