// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IValueProviderFactory
    {
        /// <summary>
        /// Get a value provider with values from the given <paramref name="requestContext"/>.
        /// </summary>
        /// <param name="routeContext">RouteContext that value provider will populate from</param>
        /// <returns>a value provider instance or null</returns>
        IValueProvider GetValueProvider([NotNull] RouteContext routeContext);
    }
}
