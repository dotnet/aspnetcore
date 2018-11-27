// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    /// <summary>
    /// Indicates actions in a controller should be selected only if all non-optional parameters are satisfied. Applies
    /// the <see cref="OverloadActionConstraint"/> to all actions in the controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class UseWebApiOverloadingAttribute : Attribute, IUseWebApiOverloading
    {
    }
}