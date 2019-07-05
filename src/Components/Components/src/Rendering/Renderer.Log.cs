// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public abstract partial class Renderer
    {
        internal static class Log
        {
            private static readonly Action<ILogger, int, Type, Exception> _renderingComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Trace, new EventId(2, "RenderingComponent"), "Rendering component {componentId} of type {componentType}");

            public static void RenderingComponent(ILogger logger, ComponentState componentState)
                => _renderingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
        }
    }
}
