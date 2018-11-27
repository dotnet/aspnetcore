// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiParameterConventionsApplicationModelConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (IsConventionApplicable(action.Controller))
            {
                var optionalParameters = new HashSet<string>();
                var uriBindingSource = (new FromUriAttribute()).BindingSource;
                foreach (var parameter in action.Parameters)
                {
                    // Some IBindingSourceMetadata attributes like ModelBinder attribute return null 
                    // as their binding source. Special case to ensure we do not ignore them.
                    if (parameter.BindingInfo?.BindingSource != null ||
                        parameter.Attributes.OfType<IBindingSourceMetadata>().Any())
                    {
                        // This has a binding behavior configured, just leave it alone.
                    }
                    else if (CanConvertFromString(parameter.ParameterInfo.ParameterType))
                    {
                        // Simple types are by-default from the URI.
                        parameter.BindingInfo = parameter.BindingInfo ?? new BindingInfo();
                        parameter.BindingInfo.BindingSource = uriBindingSource;
                    }
                    else
                    {
                        // Complex types are by-default from the body.
                        parameter.BindingInfo = parameter.BindingInfo ?? new BindingInfo();
                        parameter.BindingInfo.BindingSource = BindingSource.Body;
                    }

                    // For all non IOptionalBinderMetadata, which are not URL source (like FromQuery etc.) do not
                    // participate in overload selection and hence are added to the hashset so that they can be
                    // ignored in OverloadActionConstraint.
                    var optionalMetadata = parameter.Attributes.OfType<IOptionalBinderMetadata>().SingleOrDefault();
                    if (parameter.ParameterInfo.HasDefaultValue && parameter.BindingInfo.BindingSource == uriBindingSource ||
                        optionalMetadata != null && optionalMetadata.IsOptional ||
                        optionalMetadata == null && parameter.BindingInfo.BindingSource != uriBindingSource)
                    {
                        optionalParameters.Add(parameter.ParameterName);
                    }
                }

                action.Properties.Add("OptionalParameters", optionalParameters);
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiParameterConventions>().Any();
        }

        private static bool CanConvertFromString(Type destinationType)
        {
            destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
            return IsSimpleType(destinationType) ||
                   TypeDescriptor.GetConverter(destinationType).CanConvertFrom(typeof(string));
        }

        private static bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                type.Equals(typeof(decimal)) ||
                type.Equals(typeof(string)) ||
                type.Equals(typeof(DateTime)) ||
                type.Equals(typeof(Guid)) ||
                type.Equals(typeof(DateTimeOffset)) ||
                type.Equals(typeof(TimeSpan)) ||
                type.Equals(typeof(Uri));
        }
    }
}