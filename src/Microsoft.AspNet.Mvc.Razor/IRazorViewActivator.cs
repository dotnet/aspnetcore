// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Provides methods to activate properties on a view instance.
    /// </summary>
    public interface IRazorViewActivator
    {
        /// <summary>
        /// When implemented in a type, activates an instantiated view.
        /// </summary>
        /// <param name="view">The view to activate.</param>
        /// <param name="context">The <see cref="ViewContext"/> for the view.</param>
        void Activate(RazorView view, ViewContext context);
    }
}