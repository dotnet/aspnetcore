// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Interface for attributes which can supply a route template for attribute routing.
    /// </summary>
    public interface IRouteTemplateProvider
    {
        /// <summary>
        /// The route template. May be null.
        /// </summary>
        string Template { get; }
    }
}