// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiOverloadingApplicationModelConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (IsConventionApplicable(action.Controller))
            {
                foreach (var actionSelectorModel in action.Selectors)
                {
                    actionSelectorModel.ActionConstraints.Add(new OverloadActionConstraint());
                }
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiOverloading>().Any();
        }
    }
}