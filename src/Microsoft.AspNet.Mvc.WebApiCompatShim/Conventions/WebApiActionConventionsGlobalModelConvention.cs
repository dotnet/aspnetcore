// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiActionConventionsGlobalModelConvention : IGlobalModelConvention
    {
        private static readonly string[] SupportedHttpMethodConventions = new string[]
        {
            "GET",
            "PUT",
            "POST",
            "DELETE",
            "PATCH",
            "HEAD",
            "OPTIONS",
        };

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
            return controller.Attributes.OfType<IUseWebApiActionConventions>().Any();
        }

        private void Apply(ControllerModel model)
        {
            var newActions = new List<ActionModel>();

            foreach (var action in model.Actions)
            {
                SetHttpMethodFromConvention(action);

                // Action Name doesn't really come into play with attribute routed actions. However for a
                // non-attribute-routed action we need to create a 'named' version and an 'unnamed' version.
                if (!IsActionAttributeRouted(action))
                {
                    var namedAction = action;

                    var unnamedAction = new ActionModel(namedAction);
                    unnamedAction.IsActionNameMatchRequired = false;
                    newActions.Add(unnamedAction);
                }
            }

            model.Actions.AddRange(newActions);
        }

        private bool IsActionAttributeRouted(ActionModel action)
        {
            if (action.Controller.AttributeRoutes.Count > 0)
            {
                return true;
            }

            return action.AttributeRouteModel?.Template != null;
        }

        private void SetHttpMethodFromConvention(ActionModel action)
        {
            if (action.HttpMethods.Count > 0)
            {
                // If the HttpMethods are set from attributes, don't override it with the convention
                return;
            }

            // The Method name is used to infer verb constraints. Changing the action name has not impact.
            foreach (var verb in SupportedHttpMethodConventions)
            {
                if (action.ActionMethod.Name.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                {
                    action.HttpMethods.Add(verb);
                    return;
                }
            }

            // If no convention matches, then assume POST
            action.HttpMethods.Add("POST");
        }
    }
}