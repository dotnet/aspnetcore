// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Description
{
    /// <summary>
    /// Represents visibility metadata for an <see cref="ApiDescription"/>.
    /// </summary>
    public interface IApiDescriptionVisibilityProvider
    {
        /// <summary>
        /// If <c>false</c> then no <see cref="ApiDescription"/> objects will be created for the associated controller
        /// or action.
        /// </summary>
        bool IgnoreApi { get; }
    }
}