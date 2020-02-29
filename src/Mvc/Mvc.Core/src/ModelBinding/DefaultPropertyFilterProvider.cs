// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Default implementation for <see cref="IPropertyFilterProvider"/>.
    /// Provides a expression based way to provide include properties.
    /// </summary>
    /// <typeparam name="TModel">The target model Type.</typeparam>
    public class DefaultPropertyFilterProvider<TModel> : IPropertyFilterProvider
        where TModel : class
    {
        private static readonly Func<ModelMetadata, bool> _default = (m) => true;

        /// <summary>
        /// The prefix which is used while generating the property filter.
        /// </summary>
        public virtual string Prefix => string.Empty;

        /// <summary>
        /// Expressions which can be used to generate property filter which can filter model 
        /// properties.
        /// </summary>
        public virtual IEnumerable<Expression<Func<TModel, object>>> PropertyIncludeExpressions => null;

        /// <inheritdoc />
        public virtual Func<ModelMetadata, bool> PropertyFilter
        {
            get
            {
                if (PropertyIncludeExpressions == null)
                {
                    return _default;
                }

                // We do not cache by default.
                return GetPropertyFilterFromExpression(PropertyIncludeExpressions);
            }
        }

        private Func<ModelMetadata, bool> GetPropertyFilterFromExpression(
            IEnumerable<Expression<Func<TModel, object>>> includeExpressions)
        {
            var expression = ModelBindingHelper.GetPropertyFilterExpression(includeExpressions.ToArray());
            return expression.Compile();
        }
    }
}
