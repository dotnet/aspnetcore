// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A <see cref="IActionModelConvention"/> that sets Api Explorer visibility.
    /// </summary>
    public class ApiVisibilityConvention : IActionModelConvention
    {
        /// <inheritdoc />
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

        /// <summary>
        /// Determines if this instance of <see cref="IActionModelConvention"/> applies to a specified <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The <see cref="ActionModel"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the convention applies, otherwise <see langword="false"/>.
        /// Derived types may override this method to selectively apply this convention.
        /// </returns>
        protected virtual bool ShouldApply(ActionModel action) => true;
    }
}
