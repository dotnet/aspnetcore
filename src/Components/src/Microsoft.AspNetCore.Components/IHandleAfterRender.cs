// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Interface implemented by components that receive notification that they have been rendered.
    /// </summary>
    public interface IHandleAfterRender
    {
        /// <summary>
        /// Notifies the component that it has been rendered.
        /// </summary>
        void OnAfterRender();
    }
}
