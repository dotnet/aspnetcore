// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An <see cref="IPageRouteModelConvention"/> that sets page route resolution
    /// to use the specified <see cref="IOutboundParameterTransformer"/> on <see cref="PageRouteModel"/>.
    /// This convention does not effect controller action routes.
    /// </summary>
    public class PageRouteTransformerConvention : IPageRouteModelConvention
    {
        private IOutboundParameterTransformer _parameterTransformer;

        /// <summary>
        /// Creates a new instance of <see cref="PageRouteTransformerConvention"/> with the specified <see cref="IOutboundParameterTransformer"/>.
        /// </summary>
        /// <param name="parameterTransformer">The <see cref="IOutboundParameterTransformer"/> to use resolve page routes.</param>
        public PageRouteTransformerConvention(IOutboundParameterTransformer parameterTransformer)
        {
            if (parameterTransformer == null)
            {
                throw new ArgumentNullException(nameof(parameterTransformer));
            }

            _parameterTransformer = parameterTransformer;
        }

        public void Apply(PageRouteModel model)
        {
            if (ShouldApply(model))
            {
                model.RouteParameterTransformer = _parameterTransformer;
            }
        }

        protected virtual bool ShouldApply(PageRouteModel action) => true;
    }
}
