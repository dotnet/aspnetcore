// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.Mvc.ApplicationModel;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiParameterConventionsGlobalModelConvention : IGlobalModelConvention
    {
        public void Apply(GlobalModel model)
        {
            foreach (var controller in model.Controllers)
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

        private void Apply(ControllerModel model)
        {
            foreach (var action in model.Actions)
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
                }
            }
        }
    }
}