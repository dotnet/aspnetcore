// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// A context object for <see cref="ApiDescription"/> providers.
    /// </summary>
    public class ApiDescriptionProviderContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="ApiDescriptionProviderContext"/>.
        /// </summary>
        /// <param name="actions">The list of actions.</param>
        public ApiDescriptionProviderContext(IReadOnlyList<ActionDescriptor> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            Actions = actions;

            Results = new List<ApiDescription>();
        }

        /// <summary>
        /// The list of actions.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> Actions { get; }

        /// <summary>
        /// The list of resulting <see cref="ApiDescription"/>.
        /// </summary>
        public IList<ApiDescription> Results { get; }
    }
}