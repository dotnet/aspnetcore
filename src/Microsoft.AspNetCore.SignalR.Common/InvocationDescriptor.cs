// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    public class InvocationDescriptor : InvocationMessage
    {
        public string Method { get; set; }

        public object[] Arguments { get; set; }

        public override string ToString()
        {
            return $"{Id}: {Method}({(Arguments ?? new object[0]).Length})";
        }
    }
}
