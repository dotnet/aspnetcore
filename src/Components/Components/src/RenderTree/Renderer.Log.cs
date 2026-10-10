// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.RenderTree;

public abstract partial class Renderer
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Initializing component {ComponentId} ({ComponentType}) as child of {ParentComponentId} ({ParentComponentType})", EventName = "InitializingChildComponent", SkipEnabledCheck = true)]
        private static partial void InitializingChildComponent(ILogger logger, int componentId, Type componentType, int parentComponentId, Type parentComponentType);

        [LoggerMessage(2, LogLevel.Debug, "Initializing root component {ComponentId} ({ComponentType})", EventName = "InitializingRootComponent", SkipEnabledCheck = true)]
        private static partial void InitializingRootComponent(ILogger logger, int componentId, Type componentType);

        public static void InitializingComponent(ILogger logger, ComponentState componentState, ComponentState parentComponentState)
        {
            if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
            {
                if (parentComponentState == null)
                {
                    InitializingRootComponent(logger, componentState.ComponentId, componentState.Component.GetType());
                }
                else
                {
                    InitializingChildComponent(logger, componentState.ComponentId, componentState.Component.GetType(), parentComponentState.ComponentId, parentComponentState.Component.GetType());
                }
            }
        }

        [LoggerMessage(3, LogLevel.Debug, "Rendering component {ComponentId} of type {ComponentType}", EventName = "RenderingComponent", SkipEnabledCheck = true)]
        private static partial void RenderingComponent(ILogger logger, int componentId, Type componentType);

        public static void RenderingComponent(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
            {
                RenderingComponent(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }

        [LoggerMessage(4, LogLevel.Debug, "Disposing component {ComponentId} of type {ComponentType}", EventName = "DisposingComponent", SkipEnabledCheck = true)]
        private static partial void DisposingComponent(ILogger logger, int componentId, Type componentType);

        public static void DisposingComponent(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
            {
                DisposingComponent(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }

        [LoggerMessage(5, LogLevel.Debug, "Handling event {EventId} of type '{EventType}'", EventName = "HandlingEvent", SkipEnabledCheck = true)]
        public static partial void HandlingEvent(ILogger logger, ulong eventId, string eventType);

        public static void HandlingEvent(ILogger logger, ulong eventHandlerId, EventArgs? eventArgs)
        {
            if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
            {
                HandlingEvent(logger, eventHandlerId, eventArgs?.GetType().Name ?? "null");
            }
        }

        [LoggerMessage(6, LogLevel.Debug, "Skipping attempt to raise event {EventId} of type '{EventType}' because the component ID {ComponentId} was already disposed", EventName = "SkippingEventOnDisposedComponent", SkipEnabledCheck = true)]
        public static partial void SkippingEventOnDisposedComponent(ILogger logger, int componentId, ulong eventId, string eventType);

        public static void SkippingEventOnDisposedComponent(ILogger logger, int componentId, ulong eventHandlerId, EventArgs? eventArgs)
        {
            if (logger.IsEnabled(LogLevel.Debug)) // This is almost always false, so skip the evaluations
            {
                SkippingEventOnDisposedComponent(logger, componentId, eventHandlerId, eventArgs?.GetType().Name ?? "null");
            }
        }

        [LoggerMessage(7, LogLevel.Debug, "Skipping cascading parameter update for component {ComponentId} ({ComponentType}): component was already disposed", EventName = "SkippingCascadingUpdateOnDisposedComponent", SkipEnabledCheck = true)]
        private static partial void SkippingCascadingUpdateOnDisposedComponent_Impl(ILogger logger, int componentId, Type componentType);

        public static void SkippingCascadingUpdateOnDisposedComponent(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                SkippingCascadingUpdateOnDisposedComponent_Impl(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }

        [LoggerMessage(8, LogLevel.Debug, "Component {ComponentId} ({ComponentType}) stopped receiving single-delivery cascading parameters", EventName = "StoppedSingleDeliveryCascadingParameters", SkipEnabledCheck = true)]
        private static partial void StoppedSingleDeliveryCascadingParameters_Impl(ILogger logger, int componentId, Type componentType);

        public static void StoppedSingleDeliveryCascadingParameters(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StoppedSingleDeliveryCascadingParameters_Impl(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }

        [LoggerMessage(9, LogLevel.Debug, "Skipping render for component {ComponentId} ({ComponentType}): component was already disposed", EventName = "SkippingRenderOnDisposedComponent", SkipEnabledCheck = true)]
        private static partial void SkippingRenderOnDisposedComponent_Impl(ILogger logger, int componentId, Type componentType);

        public static void SkippingRenderOnDisposedComponent(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                SkippingRenderOnDisposedComponent_Impl(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }

        [LoggerMessage(10, LogLevel.Trace, "Supplying combined parameters to component {ComponentId} ({ComponentType})", EventName = "SupplyingCombinedParameters", SkipEnabledCheck = true)]
        private static partial void SupplyingCombinedParameters_Impl(ILogger logger, int componentId, Type componentType);

        public static void SupplyingCombinedParameters(ILogger logger, ComponentState componentState)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                SupplyingCombinedParameters_Impl(logger, componentState.ComponentId, componentState.Component.GetType());
            }
        }
    }
}
