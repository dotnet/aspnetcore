// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// Indicates the controller is in the "api" area.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class UseWebApiRoutesAttribute : Attribute, IUseWebApiRoutes
    {
    }
}