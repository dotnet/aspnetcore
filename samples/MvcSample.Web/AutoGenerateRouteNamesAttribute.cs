// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ApplicationModel;

namespace MvcSample.Web
{
    // Adds an auto-generated route-name to each action in the controller
    public class AutoGenerateRouteNamesAttribute : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel model)
        {
            foreach (var action in model.Actions)
            {
                if (action.AttributeRouteModel == null)
                {
                    action.AttributeRouteModel = new AttributeRouteModel();
                }

                if (action.AttributeRouteModel.Name == null)
                {
                    action.AttributeRouteModel.Name = string.Format(
                        "{0}_{1}", 
                        model.ControllerName, 
                        action.ActionName);
                }
            }
        }
    }
}