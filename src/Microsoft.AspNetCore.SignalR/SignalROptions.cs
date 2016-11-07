// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class SignalROptions
    {
        internal readonly Dictionary<string, Type> _invocationMappings = new Dictionary<string, Type>();

        public void RegisterInvocationAdapter<TInvocationAdapter>(string format) where TInvocationAdapter : IInvocationAdapter
        {
            _invocationMappings[format] = typeof(TInvocationAdapter);
        }
    }
}
