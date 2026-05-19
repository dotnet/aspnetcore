// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

public class ActionDescriptionAttribute : Attribute, IActionModelConvention
{
    private readonly object _value;

    public ActionDescriptionAttribute(object value)
    {
        _value = value;
    }

    public void Apply(ActionModel model)
    {
        model.Properties["description"] = _value;
    }
}
