// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Interface implemented by components that receive notifications when their
    /// properties are changed by their parent component.
    /// </summary>
    public interface IHandlePropertiesChanged
    {
        /// <summary>
        /// Notifies the component that its properties have changed.
        /// </summary>
        void OnPropertiesChanged();
    }
}
