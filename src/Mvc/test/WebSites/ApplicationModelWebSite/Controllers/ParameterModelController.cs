// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApplicationModelWebSite;

// This controller uses a reflected model attribute to change a parameter's binder metadata.
//
// This could be accomplished by simply making an attribute that implements IBinderMetadata, but
// this is part of a test for IParameterModelConvention.
public class ParameterModelController : Controller
{
    public string GetParameterMetadata([Cool] int? id)
    {
        return ControllerContext.ActionDescriptor.Parameters[0].BindingInfo.BinderModelName;
    }

    private class CoolAttribute : Attribute, IParameterModelConvention
    {
        public void Apply(ParameterModel model)
        {
            model.BindingInfo = model.BindingInfo ?? new BindingInfo();
            model.BindingInfo.BindingSource = BindingSource.Custom;
            model.BindingInfo.BinderModelName = "CoolMetadata";
        }
    }
}
