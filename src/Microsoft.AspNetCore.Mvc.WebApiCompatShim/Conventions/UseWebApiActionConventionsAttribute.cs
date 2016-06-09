// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// Indicates actions without attribute routes in a controller use WebAPI routing conventions.                                                                                                          w
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class UseWebApiActionConventionsAttribute : Attribute, IUseWebApiActionConventions
    {
    }
}