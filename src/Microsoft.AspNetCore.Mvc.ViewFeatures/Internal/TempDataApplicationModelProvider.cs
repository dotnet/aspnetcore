// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TempDataApplicationModelProvider : IApplicationModelProvider
    {
        /// <inheritdoc />
        /// <remarks>This order ensures that <see cref="TempDataApplicationModelProvider"/> runs after the <see cref="DefaultApplicationModelProvider"/>.</remarks>
        public int Order => -1000 + 10;

        /// <inheritdoc />
        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var controllerModel in context.Result.Controllers)
            {
                SaveTempDataPropertyFilterFactory factory = null;
                var propertyHelpers = PropertyHelper.GetVisibleProperties(type: controllerModel.ControllerType.AsType());
                for (var i = 0; i < propertyHelpers.Length; i++)
                {
                    var propertyHelper = propertyHelpers[i];
                    if (propertyHelper.Property.IsDefined(typeof(TempDataAttribute)))
                    {
                        ValidateProperty(propertyHelper);
                        if (factory == null)
                        {
                            factory = new SaveTempDataPropertyFilterFactory()
                            {
                                TempDataProperties = new List<PropertyHelper>()
                            };
                        }

                        factory.TempDataProperties.Add(propertyHelper);
                    }
                }

                if (factory != null)
                {
                    controllerModel.Filters.Add(factory);
                }
            }
        }

        private void ValidateProperty(PropertyHelper propertyHelper)
        {
            var property = propertyHelper.Property;
            if (!(property.SetMethod != null &&
                property.SetMethod.IsPublic &&
                property.GetMethod != null &&
                property.GetMethod.IsPublic))
            {
                throw new InvalidOperationException(
                    Resources.FormatTempDataProperties_PublicGetterSetter(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));
            }

            if (!(property.PropertyType.GetTypeInfo().IsPrimitive || property.PropertyType == typeof(string)))
            {
                throw new InvalidOperationException(
                    Resources.FormatTempDataProperties_PrimitiveTypeOrString(property.DeclaringType.FullName, property.Name, nameof(TempDataAttribute)));
            }
        }
    }
}
