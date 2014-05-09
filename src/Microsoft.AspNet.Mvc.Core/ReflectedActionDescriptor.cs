// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    [DebuggerDisplay("CA {ControllerName}:{Name}(RC-{RouteConstraints.Count})")]
    public class ReflectedActionDescriptor : ActionDescriptor
    {
        public string ControllerName
        {
            get
            {
                return ControllerDescriptor.Name;
            }
        }

        public MethodInfo MethodInfo { get; set; }

        public ControllerDescriptor ControllerDescriptor { get; set; }
    }
}
