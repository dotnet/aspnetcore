// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal sealed class RequestContext<TContext> : RequestContext
    {
        public FeatureContext<TContext> FeatureContext { get; set; }

        public RequestContext(HttpSysListener server, NativeRequestContext memoryBlob) : base(server, memoryBlob)
        {
            FeatureContext = new FeatureContext<TContext>(this);
        }

        public void Initialize(HttpSysListener server, NativeRequestContext memoryBlob)
        {
            base.InitializeCore(server, memoryBlob);
            FeatureContext.Initialize();
        }

        public void Reset()
        {
            base.ResetCore();
            FeatureContext.Reset();
        }
    }
}
