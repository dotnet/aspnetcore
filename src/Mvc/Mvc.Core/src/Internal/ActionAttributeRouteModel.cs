// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class ActionAttributeRouteModel
    {
        public static IEnumerable<(AttributeRouteModel route, SelectorModel actionSelector, SelectorModel controllerSelector)> GetAttributeRoutes(ActionModel actionModel)
        {
            var controllerAttributeRoutes = actionModel.Controller.Selectors
                .Where(sm => sm.AttributeRouteModel != null)
                .Select(sm => sm.AttributeRouteModel)
                .ToList();

            foreach (var actionSelectorModel in actionModel.Selectors)
            {
                var actionRouteModel = actionSelectorModel.AttributeRouteModel;

                // We check the action to see if the template allows combination behavior
                // (It doesn't start with / or ~/) so that in the case where we have multiple
                // [Route] attributes on the controller we don't end up creating multiple
                if (actionRouteModel != null && actionRouteModel.IsAbsoluteTemplate)
                {
                    var route = AttributeRouteModel.CombineAttributeRouteModel(
                        left: null,
                        right: actionRouteModel);

                    yield return (route, actionSelectorModel, null);
                }
                else if (controllerAttributeRoutes.Count > 0)
                {
                    for (var i = 0; i < actionModel.Controller.Selectors.Count; i++)
                    {
                        // We're using the attribute routes from the controller
                        var controllerSelector = actionModel.Controller.Selectors[i];

                        var route = AttributeRouteModel.CombineAttributeRouteModel(
                            controllerSelector.AttributeRouteModel,
                            actionRouteModel);

                        yield return (route, actionSelectorModel, controllerSelector);
                    }
                }

                else
                {
                    var route = AttributeRouteModel.CombineAttributeRouteModel(
                        left: null,
                        right: actionRouteModel);

                    yield return (route, actionSelectorModel, null);
                }
            }
        }
    }
}
