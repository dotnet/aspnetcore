// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class VndErrorAttribute : Attribute, IFilterMetadata
    {
    }
}