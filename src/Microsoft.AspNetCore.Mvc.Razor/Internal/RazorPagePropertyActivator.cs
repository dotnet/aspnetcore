// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorPagePropertyActivator
    {
        private delegate ViewDataDictionary CreateViewDataNestedDelegate(ViewDataDictionary source);
        private delegate ViewDataDictionary CreateViewDataRootDelegate(ModelStateDictionary modelState);

        public RazorPagePropertyActivator(
            Type pageType,
            Type modelType,
            IModelMetadataProvider metadataProvider,
            PropertyValueAccessors propertyValueAccessors)
        {
            var viewDataType = typeof(ViewDataDictionary<>).MakeGenericType(modelType);
            ViewDataDictionaryType = viewDataType;
            CreateViewDataNested = GetCreateViewDataNested(viewDataType);
            CreateViewDataRoot = GetCreateViewDataRoot(viewDataType, metadataProvider);

            PropertyActivators = PropertyActivator<ViewContext>.GetPropertiesToActivate(
                    pageType,
                    typeof(RazorInjectAttribute),
                    propertyInfo => CreateActivateInfo(propertyInfo, propertyValueAccessors),
                    includeNonPublic: true);
        }

        private PropertyActivator<ViewContext>[] PropertyActivators { get; }

        private Type ViewDataDictionaryType { get; }

        private CreateViewDataNestedDelegate CreateViewDataNested { get; }

        private CreateViewDataRootDelegate CreateViewDataRoot { get; }

        public void Activate(object page, ViewContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ViewData = CreateViewDataDictionary(context);

            for (var i = 0; i < PropertyActivators.Length; i++)
            {
                var activateInfo = PropertyActivators[i];
                activateInfo.Activate(page, context);
            }
        }

        private ViewDataDictionary CreateViewDataDictionary(ViewContext context)
        {
            // Create a ViewDataDictionary<TModel> if the ViewContext.ViewData is not set or the type of
            // ViewContext.ViewData is an incompatible type.
            if (context.ViewData == null)
            {
                // Create ViewDataDictionary<TModel>(IModelMetadataProvider, ModelStateDictionary).
                return CreateViewDataRoot(context.ModelState);
            }
            else if (context.ViewData.GetType() != ViewDataDictionaryType)
            {
                // Create ViewDataDictionary<TModel>(ViewDataDictionary).
                return CreateViewDataNested(context.ViewData);
            }

            return context.ViewData;
        }

        private static CreateViewDataNestedDelegate GetCreateViewDataNested(Type viewDataDictionaryType)
        {
            var parameterTypes = new Type[] { typeof(ViewDataDictionary) };
            var matchingConstructor = viewDataDictionaryType.GetConstructor(parameterTypes);
            Debug.Assert(matchingConstructor != null);

            var parameters = new ParameterExpression[] { Expression.Parameter(parameterTypes[0]) };
            var newExpression = Expression.New(matchingConstructor, parameters);
            var castNewCall = Expression.Convert(
                newExpression,
                typeof(ViewDataDictionary));
            var lambda = Expression.Lambda<CreateViewDataNestedDelegate>(castNewCall, parameters);
            return lambda.Compile();
        }

        private static CreateViewDataRootDelegate GetCreateViewDataRoot(
            Type viewDataDictionaryType,
            IModelMetadataProvider provider)
        {
            var parameterTypes = new[]
            {
                typeof(IModelMetadataProvider),
                typeof(ModelStateDictionary)
            };
            var matchingConstructor = viewDataDictionaryType.GetConstructor(parameterTypes);
            Debug.Assert(matchingConstructor != null);

            var parameterExpression = Expression.Parameter(parameterTypes[1]);
            var parameters = new Expression[]
            {
                Expression.Constant(provider),
                parameterExpression
            };
            var newExpression = Expression.New(matchingConstructor, parameters);
            var castNewCall = Expression.Convert(
                newExpression,
                typeof(ViewDataDictionary));
            var lambda = Expression.Lambda<CreateViewDataRootDelegate>(castNewCall, parameterExpression);
            return lambda.Compile();
        }

        private static PropertyActivator<ViewContext> CreateActivateInfo(
            PropertyInfo property,
            PropertyValueAccessors valueAccessors)
        {
            Func<ViewContext, object> valueAccessor;
            if (typeof(ViewDataDictionary).IsAssignableFrom(property.PropertyType))
            {
                // Logic looks reversed in condition above but is OK. Support only properties of base
                // ViewDataDictionary type and activationInfo.ViewDataDictionaryType. VDD<AnotherType> will fail when
                // assigning to the property (InvalidCastException) and that's fine.
                valueAccessor = context => context.ViewData;
            }
            else if (property.PropertyType == typeof(IUrlHelper))
            {
                // W.r.t. specificity of above condition: Users are much more likely to inject their own
                // IUrlHelperFactory than to create a class implementing IUrlHelper (or a sub-interface) and inject
                // that. But the second scenario is supported. (Note the class must implement ICanHasViewContext.)
                valueAccessor = valueAccessors.UrlHelperAccessor;
            }
            else if (property.PropertyType == typeof(IJsonHelper))
            {
                valueAccessor = valueAccessors.JsonHelperAccessor;
            }
            else if (property.PropertyType == typeof(DiagnosticSource))
            {
                valueAccessor = valueAccessors.DiagnosticSourceAccessor;
            }
            else if (property.PropertyType == typeof(HtmlEncoder))
            {
                valueAccessor = valueAccessors.HtmlEncoderAccessor;
            }
            else if (property.PropertyType == typeof(IModelExpressionProvider))
            {
                valueAccessor = valueAccessors.ModelExpressionProviderAccessor;
            }
            else
            {
                valueAccessor = context =>
                {
                    var serviceProvider = context.HttpContext.RequestServices;
                    var value = serviceProvider.GetRequiredService(property.PropertyType);
                    (value as IViewContextAware)?.Contextualize(context);

                    return value;
                };
            }

            return new PropertyActivator<ViewContext>(property, valueAccessor);
        }

        public class PropertyValueAccessors
        {
            public Func<ViewContext, object> UrlHelperAccessor { get; set; }

            public Func<ViewContext, object> JsonHelperAccessor { get; set; }

            public Func<ViewContext, object> DiagnosticSourceAccessor { get; set; }

            public Func<ViewContext, object> HtmlEncoderAccessor { get; set; }

            public Func<ViewContext, object> ModelExpressionProviderAccessor { get; set; }
        }
    }
}
