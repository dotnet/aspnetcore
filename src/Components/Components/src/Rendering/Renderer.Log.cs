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
            private const LogLevel RendererLogLevel = LogLevel.Debug;

            private static readonly Action<ILogger, int, Type, int, Type, Exception> _initializingChildComponent =
                LoggerMessage.Define<int, Type, int, Type>(RendererLogLevel, new EventId(1, "InitializingChildComponent"), "Initializing component {componentId} ({componentType}) as child of {parentComponentId} ({parentComponentId})");

            private static readonly Action<ILogger, int, Type, Exception> _initializingRootComponent =
                LoggerMessage.Define<int, Type>(RendererLogLevel, new EventId(2, "InitializingRootComponent"), "Initializing root component {componentId} ({componentType})");

            private static readonly Action<ILogger, int, Type, Exception> _renderingComponent =
                LoggerMessage.Define<int, Type>(RendererLogLevel, new EventId(3, "RenderingComponent"), "Rendering component {componentId} of type {componentType}");

            private static readonly Action<ILogger, int, Type, Exception> _disposingComponent =
                LoggerMessage.Define<int, Type>(RendererLogLevel, new EventId(4, "DisposingComponent"), "Disposing component {componentId} of type {componentType}");

            public static void InitializingComponent(ILogger logger, ComponentState componentState, ComponentState parentComponentState)
            {
                if (logger.IsEnabled(RendererLogLevel)) // This is almost always false, so skip the evaluations
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
                if (logger.IsEnabled(RendererLogLevel)) // This is almost always false, so skip the evaluations
                {
                    _renderingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                }
            }

            internal static void DisposingComponent(ILogger<Renderer> logger, ComponentState componentState)
            {
                if (logger.IsEnabled(RendererLogLevel)) // This is almost always false, so skip the evaluations
                {
                    _disposingComponent(logger, componentState.ComponentId, componentState.Component.GetType(), null);
                }
            }
        }
    }
}
