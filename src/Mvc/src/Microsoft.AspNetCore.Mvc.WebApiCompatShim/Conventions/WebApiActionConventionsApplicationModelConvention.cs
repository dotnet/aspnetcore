// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiActionConventionsApplicationModelConvention : IControllerModelConvention
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

        public void Apply(ControllerModel controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (IsConventionApplicable(controller))
            {
                var newActions = new List<ActionModel>();

                foreach (var action in controller.Actions)
                {
                    SetHttpMethodFromConvention(action);

                    // Action Name doesn't really come into play with attribute routed actions. However for a
                    // non-attribute-routed action we need to create a 'named' version and an 'unnamed' version.
                    if (!IsActionAttributeRouted(action))
                    {
                        var namedAction = action;

                        var unnamedAction = new ActionModel(namedAction);
                        unnamedAction.RouteValues.Add("action", null);
                        newActions.Add(unnamedAction);
                    }
                }

                foreach (var action in newActions)
                {
                    controller.Actions.Add(action);
                }
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiActionConventions>().Any();
        }

        private bool IsActionAttributeRouted(ActionModel action)
        {
            foreach (var controllerSelectorModel in action.Controller.Selectors)
            {
                if (controllerSelectorModel.AttributeRouteModel?.Template != null)
                {
                    return true;
                }
            }

            foreach (var actionSelectorModel in action.Selectors)
            {
                if (actionSelectorModel.AttributeRouteModel?.Template != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetHttpMethodFromConvention(ActionModel action)
        {
            foreach (var selector in action.Selectors)
            {
                if (selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Count() > 0)
                {
                    // If the HttpMethods are set from attributes, don't override it with the convention
                    return;
                }
            }

            // The Method name is used to infer verb constraints. Changing the action name has no impact.
            foreach (var verb in SupportedHttpMethodConventions)
            {
                if (action.ActionMethod.Name.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var selector in action.Selectors)
                    {
                        selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { verb }));
                    }

                    return;
                }
            }

            // If no convention matches, then assume POST
            foreach (var actionSelectorModel in action.Selectors)
            {
                actionSelectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { "POST" }));
            }
        }
    }
}
