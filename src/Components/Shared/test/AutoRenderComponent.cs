// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public abstract class AutoRenderComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public virtual Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            TriggerRender();
            return Task.CompletedTask;
        }

        // We do it this way so that we don't have to be doing renderer.Invoke on each and every test.
        public void TriggerRender()
        {
            var t = _renderHandle.Invoke(() => _renderHandle.Render(BuildRenderTree));
            // This should always be run synchronously
            Assert.True(t.IsCompleted);
            if (t.IsFaulted)
            {
                var exception = t.Exception.Flatten().InnerException;
                while (exception is AggregateException e)
                {
                    exception = e.InnerException;
                }
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        protected abstract void BuildRenderTree(RenderTreeBuilder builder);
    }
}
