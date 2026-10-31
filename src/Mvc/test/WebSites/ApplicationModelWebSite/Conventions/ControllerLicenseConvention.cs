// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

public class ControllerLicenseConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        controller.Properties["license"] = @"Licensed to the .NET Foundation under one or more agreements.
The .NET Foundation licenses this file to you under the MIT license.";
    }
}
