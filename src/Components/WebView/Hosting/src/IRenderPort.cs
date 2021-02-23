// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.WebView.Hosting
{
    public interface IRenderPort
    {
        void Attach(IServiceProvider scope);
        Task ApplyBatchAsync(RenderBatch renderBatch);
        void OnException(Exception exception);
        void AttachRootComponent(int componentId, string selector);
    }
}
