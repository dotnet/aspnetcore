// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.HttpRepl.OpenApi
{
    public class Parameter
    {
        public string Name { get; set; }

        public string Location { get; set; }

        public bool IsRequired { get; set; }

        public Schema Schema { get; set; }
    }
}
