// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
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
                action.ActionConstraints.Add(new OverloadActionConstraint());
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiOverloading>().Any();
        }
    }
}