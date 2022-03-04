// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An interface that allows a top-level model to be bound or not bound based on state associated
    /// with the current request.
    /// </summary>
    public interface IRequestPredicateProvider
    {
        /// <summary>
        /// Gets a function which determines whether or not the model object should be bound based
        /// on the current request.
        /// </summary>
        Func<ActionContext, bool> RequestPredicate { get; }
    }
}
