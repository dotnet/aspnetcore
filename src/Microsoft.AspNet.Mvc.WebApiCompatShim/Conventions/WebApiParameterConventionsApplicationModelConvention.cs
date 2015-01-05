// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiParameterConventionsApplicationModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                if (IsConventionApplicable(controller))
                {
                    Apply(controller);
                }
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiParameterConventions>().Any();
        }

        private void Apply(ControllerModel controller)
        {
            foreach (var action in controller.Actions)
            {
                foreach (var parameter in action.Parameters)
                {
                    if (parameter.BinderMetadata is IBinderMetadata)
                    {
                        // This has a binding behavior configured, just leave it alone.
                    }
                    else if (ValueProviderResult.CanConvertFromString(parameter.ParameterInfo.ParameterType))
                    {
                        // Simple types are by-default from the URI.
                        parameter.BinderMetadata = new FromUriAttribute();
                    }
                    else
                    {
                        // Complex types are by-default from the body.
                        parameter.BinderMetadata = new FromBodyAttribute();
                    }

                    // If the parameter has a default value, we want to consider it as optional parameter by default.
                    var optionalMetadata = parameter.BinderMetadata as FromUriAttribute;
                    if (parameter.ParameterInfo.HasDefaultValue && optionalMetadata != null)
                    {
                        optionalMetadata.IsOptional = true;
                    }
                }
            }
        }
    }
}