// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A <see cref="IActionModelConvention"/> that sets Api Explorer visibility.
    /// </summary>
    public class ApiVisibilityConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (!ShouldApply(action))
            {
                return;
            }

            if (action.Controller.ApiExplorer.IsVisible == null && action.ApiExplorer.IsVisible == null)
            {
                // Enable ApiExplorer for the action if it wasn't already explicitly configured.
                action.ApiExplorer.IsVisible = true;
            }
        }

        protected virtual bool ShouldApply(ActionModel action) => true;
    }
}
