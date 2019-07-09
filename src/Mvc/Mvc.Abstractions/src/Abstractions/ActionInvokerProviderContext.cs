// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Abstractions
{
    /// <summary>
    /// A context for <see cref="IActionInvokerProvider"/>.
    /// </summary>
    public class ActionInvokerProviderContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ActionInvokerProviderContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> to invoke.</param>
        public ActionInvokerProviderContext(ActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            ActionContext = actionContext;
        }

        /// <summary>
        /// Gets the <see cref="Mvc.ActionContext"/> to invoke.
        /// </summary>
        public ActionContext ActionContext { get; }

        /// <summary>
        /// Gets or sets the <see cref="IActionInvoker"/> that will be used to invoke <see cref="ActionContext" />
        /// </summary>
        public IActionInvoker Result { get; set; }
    }
}
