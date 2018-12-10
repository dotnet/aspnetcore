// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    [DebuggerDisplay("{DisplayName}")]
    public class ControllerActionDescriptor : ActionDescriptor
    {
        public string ControllerName { get; set; }

        public virtual string ActionName { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public TypeInfo ControllerTypeInfo { get; set; }

        public override string DisplayName
        {
            get
            {
                if (base.DisplayName == null && ControllerTypeInfo != null && MethodInfo != null)
                {
                    base.DisplayName = GetDefaultDisplayName(ControllerTypeInfo, MethodInfo);
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

        internal static string GetDefaultDisplayName(Type controllerType, MethodInfo actionMethod)
        {
            if (controllerType == null)
            {
                throw new ArgumentNullException(nameof(controllerType));
            }

            if (actionMethod == null)
            {
                throw new ArgumentNullException(nameof(actionMethod));
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1} ({2})",
                TypeNameHelper.GetTypeDisplayName(controllerType),
                actionMethod.Name,
                controllerType.Assembly.GetName().Name);
        }
    }
}
