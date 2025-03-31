// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace TestContentPackage.Services;

public class InteractiveWebAssemblyService
{
    [SupplyParameterFromPersistentComponentState]
    public string State { get; set; }
}
