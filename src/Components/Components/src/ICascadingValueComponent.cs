// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;
using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Defines the interface for providing one or more cascading values from a component.
    /// </summary>
    public interface ICascadingValueComponent
    {
        /// <summary>
        /// Check if this component exposes a value for the given type and optional name combination.
        /// </summary>
        /// <param name="valueType">The expected type of underlying value</param>
        /// <param name="valueName">The optional name of the underlying value</param>
        /// <returns></returns>
        bool HasValue(Type valueType, string? valueName);

        /// <summary>
        /// Get the value from the component for the given type and optional name combination.
        /// </summary>
        /// <param name="valueType">The expected type of underlying value</param>
        /// <param name="valueName">The optional name of the underlying value</param>
        /// <returns></returns>
        object? GetValue(Type valueType, string? valueName);

        /// <summary>
        /// If true, indicates that the values from <see cref="GetValue"/> will not change.
        /// This is a performance optimization that allows the framework to skip setting up
        /// change notifications. Set this flag only if you will not change the values provded by this component.
        /// </summary>
        bool IsFixed { get; }

        /// <summary>
        /// When a change happens inside of the <see cref="ICascadingValueComponent"/> consuming components will
        /// subscribe so that your component can notify all consumers of changes.
        /// </summary>
        /// <param name="subscriber"></param>
        void Subscribe(IComponentState subscriber);

        /// <summary>
        /// When a component is no longer consuming values from the <see cref="ICascadingValueComponent"/> the
        /// component will unsubscribe to changes.
        /// </summary>
        /// <param name="subscriber"></param>
        void Unsubscribe(IComponentState subscriber);
    }
}
