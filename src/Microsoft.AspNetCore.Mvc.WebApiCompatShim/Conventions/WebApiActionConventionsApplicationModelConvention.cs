// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
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
                        unnamedAction.RouteConstraints.Add(new UnnamedActionRouteConstraint());
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

        private class UnnamedActionRouteConstraint : IRouteConstraintProvider
        {
            public UnnamedActionRouteConstraint()
            {
                RouteKey = "action";
                RouteKeyHandling = RouteKeyHandling.DenyKey;
                RouteValue = null;
            }

            public string RouteKey { get; }

            public RouteKeyHandling RouteKeyHandling { get; }

            public string RouteValue { get; }

            public bool BlockNonAttributedActions { get; }
        }
    }
}
