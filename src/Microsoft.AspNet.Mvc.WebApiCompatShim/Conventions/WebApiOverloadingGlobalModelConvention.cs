// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiOverloadingGlobalModelConvention : IGlobalModelConvention
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
            return controller.Attributes.OfType<IUseWebApiOverloading>().Any();
        }

        private void Apply(ControllerModel model)
        {
            foreach (var action in model.Actions)
            {
                action.ActionConstraints.Add(new OverloadActionConstraint());
            }
        }
    }
}