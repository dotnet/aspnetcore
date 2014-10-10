// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiRoutesGlobalModelConvention : IGlobalModelConvention
    {
        private readonly string _area;

        public WebApiRoutesGlobalModelConvention(string area)
        {
            _area = area;
        }

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
            return controller.Attributes.OfType<IUseWebApiRoutes>().Any();
        }

        private void Apply(ControllerModel model)
        {
            model.RouteConstraints.Add(new AreaAttribute(_area));
        }
    }
}