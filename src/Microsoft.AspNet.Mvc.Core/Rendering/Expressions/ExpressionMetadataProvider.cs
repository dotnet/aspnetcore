// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering.Expressions
{
    public static class ExpressionMetadataProvider
    {
        public static ModelMetadata FromLambdaExpression<TModel, TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            [NotNull] ViewDataDictionary<TModel> viewData,
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

            return GetMetadataFromProvider(
                modelAccessor,
                typeof(TResult),
                propertyName,
                container,
                containerType,
                metadataProvider);
        }

        public static ModelMetadata FromStringExpression(string expression,
                                                         [NotNull] ViewDataDictionary viewData,
                                                         IModelMetadataProvider metadataProvider)
        {
            if (string.IsNullOrEmpty(expression))
            {
                // Empty string really means "ModelMetadata for the current model".
                return FromModel(viewData, metadataProvider);
            }

            var viewDataInfo = ViewDataEvaluator.Eval(viewData, expression);
            Type containerType = null;
            Type modelType = null;
            Func<object> modelAccessor = null;
            string propertyName = null;
            object container = null;

            if (viewDataInfo != null)
            {
                if (viewDataInfo.Container != null)
                {
                    containerType = viewDataInfo.Container.GetType();
                    container = viewDataInfo.Container;
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
            else
            {
                //  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
                var propertyMetadata = viewData.ModelMetadata.Properties[expression];
                if (propertyMetadata != null)
                {
                    return propertyMetadata;
                }
            }

            return GetMetadataFromProvider(modelAccessor,
                                           modelType ?? typeof(string),
                                           propertyName,
                                           container,
                                           containerType,
                                           metadataProvider);
        }

        private static ModelMetadata FromModel([NotNull] ViewDataDictionary viewData,
                                               IModelMetadataProvider metadataProvider)
        {
            if (viewData.ModelMetadata.ModelType == typeof(object))
            {
                // Use common simple type rather than object so e.g. Editor() at least generates a TextBox.
                return GetMetadataFromProvider(
                    modelAccessor: null,
                    modelType: typeof(string),
                    propertyName: null,
                    container: null,
                    containerType: null,
                    metadataProvider: metadataProvider);
            }
            else
            {
                return viewData.ModelMetadata;
            }
        }

        // An IModelMetadataProvider is not required unless this method is called. Therefore other methods in this
        // class lack [NotNull] attributes for their corresponding parameter.
        private static ModelMetadata GetMetadataFromProvider(Func<object> modelAccessor,
                                                             Type modelType,
                                                             string propertyName,
                                                             object container,
                                                             Type containerType,
                                                             [NotNull] IModelMetadataProvider metadataProvider)
        {
            if (containerType != null && !string.IsNullOrEmpty(propertyName))
            {
                var metadata =
                    metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName);
                if (metadata != null)
                {
                    metadata.Container = container;
                }

                return metadata;
            }

            return metadataProvider.GetMetadataForType(modelAccessor, modelType);
        }
    }
}