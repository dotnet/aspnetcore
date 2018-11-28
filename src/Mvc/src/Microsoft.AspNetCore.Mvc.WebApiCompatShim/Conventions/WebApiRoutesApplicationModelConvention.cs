// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiRoutesApplicationModelConvention : IControllerModelConvention
    {
        private readonly string _area;

        public WebApiRoutesApplicationModelConvention(string area)
        {
            _area = area;
        }

        public void Apply(ControllerModel controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (IsConventionApplicable(controller))
            {
                controller.RouteValues.Add("area", _area);
            }
        }

        private bool IsConventionApplicable(ControllerModel controller)
        {
            return controller.Attributes.OfType<IUseWebApiRoutes>().Any();
        }
    }
}