// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ServerReliability
{
    public class ThrowingOnAfterRenderAsyncComponent : IComponent, IHandleAfterRender
    {
        public void Attach(RenderHandle renderHandle)
        {
            renderHandle.Render(builder =>
            {
                // Do nothing.
            });
        }

        public async Task OnAfterRenderAsync()
        {
            await Task.Yield();
            throw new InvalidTimeZoneException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
