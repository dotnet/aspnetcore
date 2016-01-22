// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Session
{
    public interface ISessionStore
    {
        bool IsAvailable { get; }
        void Connect();
        ISession Create(string sessionId, TimeSpan idleTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey);
    }
}