// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An <see cref="IActionModelConvention"/> that adds a <see cref="IFilterMetadata"/>
    /// to <see cref="ActionModel"/> that transforms <see cref="IClientErrorActionResult"/>.
    /// </summary>
    public class ClientErrorResultFilterConvention : IActionModelConvention
    {
        private readonly ClientErrorResultFilterFactory _filterFactory = new ClientErrorResultFilterFactory();

        /// <inheritdoc />
        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!ShouldApply(action))
            {
                return;
            }


            action.Filters.Add(_filterFactory);
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
