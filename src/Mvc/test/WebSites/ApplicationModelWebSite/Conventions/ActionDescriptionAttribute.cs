// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;


namespace ApplicationModelWebSite
{
    public class ActionDescriptionAttribute : Attribute, IActionModelConvention
    {
        private object _value;

        public ActionDescriptionAttribute(object value)
        {
            _value = value;
        }

        public void Apply(ActionModel model)
        {
            model.Properties["description"] = _value;
        }
    }
}