// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Principal;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal interface INegotiateState : IDisposable
    {
        string GetOutgoingBlob(string incomingBlob);

        bool IsCompleted { get; }

        IIdentity GetIdentity();
    }
}
