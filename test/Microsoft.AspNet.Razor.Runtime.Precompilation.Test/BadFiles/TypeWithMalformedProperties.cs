// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    public class TypeWithMalformedProperties
    {
        public DateTime DateTime { get }

        public int DateTime2 => "Hello world";

        public string CustomOrder { get; set; }
    }
}
