// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// Represents visibility metadata for an <c>ApiDescription</c>.
    /// </summary>
    public interface IApiDescriptionVisibilityProvider
    {
        /// <summary>
        /// If <c>false</c> then no <c>ApiDescription</c> objects will be created for the associated controller
        /// or action.
        /// </summary>
        bool IgnoreApi { get; }
    }
}