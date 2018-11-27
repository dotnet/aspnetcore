// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite
{
    public class FromHeaderConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            foreach (var param in action.Parameters)
            {
                if (param.Attributes.Any(p => p.GetType() == typeof(FromHeaderAttribute)))
                {
                    param.Action.Properties["source"] = "From Header";
                }
            }
        }
    }
}