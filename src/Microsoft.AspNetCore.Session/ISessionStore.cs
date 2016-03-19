// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Session
{
    public interface ISessionStore
    {
        bool IsAvailable { get; }

        ISession Create(string sessionKey, TimeSpan idleTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey);
    }
}