// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApiExplorer
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
        public ApiDescriptionProviderContext([NotNull] IReadOnlyList<ActionDescriptor> actions)
        {
            Actions = actions;

            Results = new List<ApiDescription>();
        }

        /// <summary>
        /// The list of actions.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> Actions { get; private set; }

        /// <summary>
        /// The list of resulting <see cref="ApiDescription"/>.
        /// </summary>
        public IList<ApiDescription> Results { get; private set; }
    }
}