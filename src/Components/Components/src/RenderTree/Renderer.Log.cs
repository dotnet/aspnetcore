// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    public abstract partial class Renderer
    {
        internal static class Log
        {
            private static readonly Action<ILogger, int, Type, int, Type, Exception> _initializingChildComponent =
                LoggerMessage.Define<int, Type, int, Type>(LogLevel.Debug, new EventId(1, "InitializingChildComponent"), "Initializing component {ComponentId} ({ComponentType}) as child of {ParentComponentId} ({ParentComponentId})");

            private static readonly Action<ILogger, int, Type, Exception> _initializingRootComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Debug, new EventId(2, "InitializingRootComponent"), "Initializing root component {ComponentId} ({ComponentType})");

            private static readonly Action<ILogger, int, Type, Exception> _renderingComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Debug, new EventId(3, "RenderingComponent"), "Rendering component {ComponentId} of type {ComponentType}");

            private static readonly Action<ILogger, int, Type, Exception> _disposingComponent =
                LoggerMessage.Define<int, Type>(LogLevel.Debug, new EventId(4, "DisposingComponent"), "Disposing component {ComponentId} of type {ComponentType}");

            private static readonly Action<ILogger, ulong, string, Exception> _handlingEvent =
                LoggerMessage.Define<ulong, string>(LogLevel.Debug, new EventId(5, "HandlingEvent"), "Handling event {EventId} of type '{EventType}'");

            public static void InitializingComponent(ILogger logger, ComponentState componentState, ComponentState parentComponentState)
            {
                if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
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
                if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
                {
                    _renderingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                }
            }

            public static void DisposingComponent(ILogger<Renderer> logger, ComponentState componentState)
            {
                if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
                {
                    _disposingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                }
            }

            public static void HandlingEvent(ILogger<Renderer> logger, ulong eventHandlerId, EventArgs eventArgs)
            {
                _handlingEvent(logger, eventHandlerId, eventArgs?.GetType().Name ?? "null", null);
            }
        }
    }
}
