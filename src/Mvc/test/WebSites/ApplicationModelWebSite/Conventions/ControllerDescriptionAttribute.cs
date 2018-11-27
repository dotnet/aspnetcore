// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite
{
    public class ControllerDescriptionAttribute : Attribute, IControllerModelConvention
    {
        private object _value;

        public ControllerDescriptionAttribute(object value)
        {
            _value = value;
        }

        public void Apply(ControllerModel model)
        {
            model.Properties["description"] = _value;
        }
    }
}