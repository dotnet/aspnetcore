// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.Logging.Testing
{
    public interface ITestSink
    {
        event Action<WriteContext> MessageLogged;

        event Action<BeginScopeContext> ScopeStarted;

        Func<WriteContext, bool> WriteEnabled { get; set; }

        Func<BeginScopeContext, bool> BeginEnabled { get; set; }

        IProducerConsumerCollection<BeginScopeContext> Scopes { get; set; }

        IProducerConsumerCollection<WriteContext> Writes { get; set; }

        void Write(WriteContext context);

        void Begin(BeginScopeContext context);
    }
}
