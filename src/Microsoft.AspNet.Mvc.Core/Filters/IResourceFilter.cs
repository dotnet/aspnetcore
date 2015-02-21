using System;
using Microsoft.Framework.Internal;
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter which surrounds execution of model binding, the action (and filters) and the action result
    /// (and filters).
    /// </summary>
    public interface IResourceFilter : IFilter
    {
        /// <summary>
        /// Executes the resource filter. Called before execution of the remainder of the pipeline.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        void OnResourceExecuting([NotNull] ResourceExecutingContext context);

        /// <summary>
        /// Executes the resource filter. Called after execution of the remainder of the pipeline.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutedContext"/>.</param>
        void OnResourceExecuted([NotNull] ResourceExecutedContext context);
    }
}