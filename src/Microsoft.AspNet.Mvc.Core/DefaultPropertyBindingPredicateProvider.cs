// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Default implementation for <see cref="IPropertyBindingPredicateProvider"/>.
    /// Provides a expression based way to provide include properties.
    /// </summary>
    /// <typeparam name="TModel">The target model Type.</typeparam>
    public class DefaultPropertyBindingPredicateProvider<TModel> : IPropertyBindingPredicateProvider
        where TModel : class
    {
        private static readonly Func<ModelBindingContext, string, bool> _defaultFilter =
            (context, propertyName) => true;

        /// <summary>
        /// The prefix which is used while generating the property filter.
        /// </summary>
        public virtual string Prefix
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Expressions which can be used to generate property filter which can filter model 
        /// properties.
        /// </summary>
        public virtual IEnumerable<Expression<Func<TModel, object>>> PropertyIncludeExpressions
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc />
        public virtual Func<ModelBindingContext, string, bool> PropertyFilter
        {
            get
            {
                if (PropertyIncludeExpressions == null)
                {
                    return _defaultFilter;
                }

                // We do not cache by default.
                return GetPredicateFromExpression(PropertyIncludeExpressions);
            }
        }

        private Func<ModelBindingContext, string, bool> GetPredicateFromExpression(
            IEnumerable<Expression<Func<TModel, object>>> includeExpressions)
        {
            var expression = ModelBindingHelper.GetIncludePredicateExpression(Prefix, includeExpressions.ToArray());
            return expression.Compile();
        }
    }
}
