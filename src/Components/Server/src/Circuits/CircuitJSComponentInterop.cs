// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Intended for framework use only. Not supported for use from application code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CircuitJSComponentInterop : JSComponentInterop
    {
        private readonly CircuitOptions _circuitOptions;
        private int _jsRootComponentCount;

        internal CircuitJSComponentInterop(
            JSComponentConfigurationStore configuration,
            JsonSerializerOptions jsonOptions,
            CircuitOptions circuitOptions)
            : base(configuration, jsonOptions)
        {
            _circuitOptions = circuitOptions;
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public override int AddRootComponent(string identifier, string domElementSelector)
        {
            if (_jsRootComponentCount >= _circuitOptions.RootComponents.MaxJSRootComponents)
            {
                throw new InvalidOperationException($"Cannot add further JS root components because the configured limit of {_circuitOptions.RootComponents.MaxJSRootComponents} has been reached.");
            }

            var id = base.AddRootComponent(identifier, domElementSelector);
            _jsRootComponentCount++;
            return id;
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public override void RemoveRootComponent(int componentId)
        {
            base.RemoveRootComponent(componentId);

            // It didn't throw, so the root component did exist before and was actually removed
            _jsRootComponentCount--;
        }
    }
}
