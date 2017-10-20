// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Represents the purpose for invoking an <see cref="IDispatcherValueConstraint"/>.
    /// </summary>
    public enum ConstraintPurpose
    {
        /// <summary>
        /// A request URL is being processed by the dispatcher.
        /// </summary>
        IncomingRequest,

        /// <summary>
        /// A URL is being created by a template.
        /// </summary>
        TemplateExecution,
    }
}
