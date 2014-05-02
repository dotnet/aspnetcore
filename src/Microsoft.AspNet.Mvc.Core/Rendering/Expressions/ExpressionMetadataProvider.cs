// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering.Expressions
{
    public static class ExpressionMetadataProvider
    {
        public static ModelMetadata FromLambdaExpression<TParameter, TValue>(
            [NotNull] Expression<Func<TParameter, TValue>> expression,
            [NotNull] ViewDataDictionary<TParameter> viewData,
            IModelMetadataProvider metadataProvider)
        {
            string propertyName = null;
            Type containerType = null;
            var legalExpression = false;

            // Need to verify the expression is valid; it needs to at least end in something
            // that we can convert to a meaningful string for model binding purposes

            switch (expression.Body.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    // ArrayIndex always means a single-dimensional indexer;
                    // multi-dimensional indexer is a method call to Get().
                    legalExpression = true;
                    break;

                case ExpressionType.Call:
                    // Only legal method call is a single argument indexer/DefaultMember call
                    legalExpression = ExpressionHelper.IsSingleArgumentIndexer(expression.Body);
                    break;

                case ExpressionType.MemberAccess:
                    // Property/field access is always legal
                    var memberExpression = (MemberExpression)expression.Body;
                    propertyName = memberExpression.Member is PropertyInfo ? memberExpression.Member.Name : null;
                    containerType = memberExpression.Expression.Type;
                    legalExpression = true;
                    break;

                case ExpressionType.Parameter:
                    // Parameter expression means "model => model", so we delegate to FromModel
                    return FromModel(viewData, metadataProvider);
            }

            if (!legalExpression)
            {
                throw new InvalidOperationException(Resources.TemplateHelpers_TemplateLimitations);
            }

            var container = viewData.Model;
            Func<object> modelAccessor = () =>
            {
                try
                {
                    return CachedExpressionCompiler.Process(expression)(container);
                }
                catch (NullReferenceException)
                {
                    return null;
                }
            };

            return GetMetadataFromProvider(modelAccessor, typeof(TValue), propertyName, containerType, metadataProvider);
        }

        public static ModelMetadata FromStringExpression(string expression,
                                                         [NotNull] ViewDataDictionary viewData,
                                                         IModelMetadataProvider metadataProvider)
        {
            if(string.IsNullOrEmpty(expression))
            {
                return viewData.ModelMetadata;
            }

            if (expression.Length == 0)
            {
                // Empty string really means "model metadata for the current model"
                return FromModel(viewData, metadataProvider);
            }

            var viewDataInfo = ViewDataEvaluator.Eval(viewData, expression);
            Type containerType = null;
            Type modelType = null;
            Func<object> modelAccessor = null;
            string propertyName = null;

            if (viewDataInfo != null)
            {
                if (viewDataInfo.Container != null)
                {
                    containerType = viewDataInfo.Container.GetType();
                }

                modelAccessor = () => viewDataInfo.Value;

                if (viewDataInfo.PropertyInfo != null)
                {
                    propertyName = viewDataInfo.PropertyInfo.Name;
                    modelType = viewDataInfo.PropertyInfo.PropertyType;
                }
                else if (viewDataInfo.Value != null)
                {
                    // We only need to delay accessing properties (for LINQ to SQL)
                    modelType = viewDataInfo.Value.GetType();
                }
            }
            else if (viewData.ModelMetadata != null)
            {
                //  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
                var propertyMetadata =
                    viewData.ModelMetadata.Properties.Where(p => p.PropertyName == expression).FirstOrDefault();
                if (propertyMetadata != null)
                {
                    return propertyMetadata;
                }
            }

            return GetMetadataFromProvider(modelAccessor, modelType ?? typeof(string), propertyName, containerType,
                metadataProvider);
        }

        private static ModelMetadata FromModel([NotNull] ViewDataDictionary viewData,
                                               IModelMetadataProvider metadataProvider)
        {
            return viewData.ModelMetadata ?? GetMetadataFromProvider(null, typeof(string), propertyName: null,
                containerType: null, metadataProvider: metadataProvider);
        }

        // An IModelMetadataProvider is not required unless this method is called. Therefore other methods in this
        // class lack [NotNull] attributes for their corresponding parameter.
        private static ModelMetadata GetMetadataFromProvider(Func<object> modelAccessor, Type modelType,
            string propertyName, Type containerType, [NotNull] IModelMetadataProvider metadataProvider)
        {
            if (containerType != null && !string.IsNullOrEmpty(propertyName))
            {
                return metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName);
            }

            return metadataProvider.GetMetadataForType(modelAccessor, modelType);
        }
    }
}