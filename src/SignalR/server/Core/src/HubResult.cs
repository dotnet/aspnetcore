// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public struct HubResult
    {
        public bool MethodInvoked;
        public object Result;

        public static HubResult WithResult(object result)
        {
            return new HubResult { MethodInvoked = true, Result = result };
        }

        public static HubResult NotInvoked()
        {
            return new HubResult { MethodInvoked = false, Result = null };
        }
    }
}
