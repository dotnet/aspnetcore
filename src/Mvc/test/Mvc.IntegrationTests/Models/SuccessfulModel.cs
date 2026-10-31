// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class SuccessfulModel
{
    [ModelBinder(typeof(SuccessfulModelBinder))]
    public bool IsBound { get; set; }

    public string Name { get; set; }
}
