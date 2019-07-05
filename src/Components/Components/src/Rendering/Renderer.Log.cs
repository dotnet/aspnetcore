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
            private static readonly Action<ILogger, int, Type, int, Type, Exception> _initializingChildComponent =
                LoggerMessage.Define<int, Type, int, Type>(LogLevel.Trace, new EventId(1, "InitializingChildComponent"), "Initializing component {componentId} ({componentType}) as child of {parentComponentId} ({parentComponentId})");

            private static readonly Action<ILogger, int, Type, Exception> _initializingRootComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Trace, new EventId(2, "InitializingRootComponent"), "Initializing root component {componentId} ({componentType})");

            private static readonly Action<ILogger, int, Type, Exception> _renderingComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Trace, new EventId(3, "RenderingComponent"), "Rendering component {componentId} of type {componentType}");

            public static void InitializingComponent(ILogger logger, ComponentState componentState, ComponentState parentComponentState)
            {
                if (logger.IsEnabled(LogLevel.Trace)) // This is almost always false, so skip the evaluations
                {
                    if (parentComponentState == null)
                    {
                        _initializingRootComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                    }
                    else
                    {
                        _initializingChildComponent(logger, componentState.ComponentId, componentState.Component.GetType(), parentComponentState.ComponentId, parentComponentState.Component.GetType(), null);
                    }
                }
            }

            public static void RenderingComponent(ILogger logger, ComponentState componentState)
            {
                if (logger.IsEnabled(LogLevel.Trace)) // This is almost always false, so skip the evaluations
                {
                    _renderingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                }
            }
        }
    }
}
