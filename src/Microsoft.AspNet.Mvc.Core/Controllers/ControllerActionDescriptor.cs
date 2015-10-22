// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNet.Mvc.Abstractions;

namespace Microsoft.AspNet.Mvc.Controllers
{
    [DebuggerDisplay("{DisplayName}")]
    public class ControllerActionDescriptor : ActionDescriptor
    {
        public string ControllerName { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ControllerTypeInfo { get; set; }

        public override string DisplayName
        {
            get
            {
                if (base.DisplayName == null && ControllerTypeInfo != null && MethodInfo != null)
                {
                    base.DisplayName = string.Format(
                        "{0}.{1}",
                        ControllerTypeInfo.FullName,
                        MethodInfo.Name);
                }

                return base.DisplayName;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                base.DisplayName = value;
            }
        }
    }
}
