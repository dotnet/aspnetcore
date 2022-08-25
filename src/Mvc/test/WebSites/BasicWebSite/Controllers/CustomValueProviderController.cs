// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class CustomValueProviderController : Controller
{
    [HttpGet]
    public string CustomValueProviderDisplayName(string customValueProviderDisplayName)
        => customValueProviderDisplayName;

    [HttpGet]
    public int[] CustomValueProviderIntValues(int[] customValueProviderIntValues)
        => customValueProviderIntValues;

    [HttpGet]
    public int?[] CustomValueProviderNullableIntValues(int?[] customValueProviderNullableIntValues)
        => customValueProviderNullableIntValues;

    [HttpGet]
    public string[] CustomValueProviderStringValues(string[] customValueProviderStringValues)
        => customValueProviderStringValues;
}
