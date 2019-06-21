// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubInvocationContext
    {
        public HubInvocationContext(HubCallerContext context, string hubMethodName, object[] hubMethodArguments)
        {
            HubMethodName = hubMethodName;
            HubMethodArguments = hubMethodArguments;
            Context = context;
        }

        public HubCallerContext Context { get; }
        public string HubMethodName { get; }
        public IReadOnlyList<object> HubMethodArguments { get; }
    }
}
