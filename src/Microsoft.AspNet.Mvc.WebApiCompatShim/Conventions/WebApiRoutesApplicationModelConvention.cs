// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiRoutesApplicationModelConvention : IApplicationModelConvention
    {
        private readonly string _area;

        public WebApiRoutesApplicationModelConvention(string area)
        {
            _area = area;
        }

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
            return controller.Attributes.OfType<IUseWebApiRoutes>().Any();
        }

        private void Apply(ControllerModel controller)
        {
            controller.RouteConstraints.Add(new AreaAttribute(_area));
        }
    }
}