// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite;

public class ApplicationDescription : IApplicationModelConvention
{
    private readonly string _description;

    public ApplicationDescription(string description)
    {
        _description = description;
    }

    public void Apply(ApplicationModel application)
    {
        application.Properties["description"] = _description;
    }
}
