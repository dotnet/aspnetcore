// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An <see cref="IActionModelConvention"/> that sets attribute routing token replacement
    /// to use the specified <see cref="IParameterTransformer"/> on <see cref="ActionModel"/> selectors.
    /// </summary>
    public class RouteTokenTransformerConvention : IActionModelConvention
    {
        private readonly IParameterTransformer _parameterTransformer;

        /// <summary>
        /// Creates a new instance of <see cref="RouteTokenTransformerConvention"/> with the specified <see cref="IParameterTransformer"/>.
        /// </summary>
        /// <param name="parameterTransformer">The <see cref="IParameterTransformer"/> to use with attribute routing token replacement.</param>
        public RouteTokenTransformerConvention(IParameterTransformer parameterTransformer)
        {
            if (parameterTransformer == null)
            {
                throw new ArgumentNullException(nameof(parameterTransformer));
            }

            _parameterTransformer = parameterTransformer;
        }

        public void Apply(ActionModel action)
        {
            if (ShouldApply(action))
            {
                action.Properties[typeof(IParameterTransformer)] = _parameterTransformer;
            }
        }

        protected virtual bool ShouldApply(ActionModel action) => true;
    }
}
