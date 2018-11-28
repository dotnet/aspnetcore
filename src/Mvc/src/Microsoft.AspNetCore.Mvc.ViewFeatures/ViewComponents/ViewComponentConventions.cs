// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    public static class ViewComponentConventions
    {
        public static readonly string ViewComponentSuffix = "ViewComponent";

        public static string GetComponentName(TypeInfo componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var attribute = componentType.GetCustomAttribute<ViewComponentAttribute>();
            if (attribute != null && !string.IsNullOrEmpty(attribute.Name))
            {
                var separatorIndex = attribute.Name.LastIndexOf('.');
                if (separatorIndex >= 0)
                {
                    return attribute.Name.Substring(separatorIndex + 1);
                }
                else
                {
                    return attribute.Name;
                }
            }

            return GetShortNameByConvention(componentType);
        }

        public static string GetComponentFullName(TypeInfo componentType)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var attribute = componentType.GetCustomAttribute<ViewComponentAttribute>();
            if (!string.IsNullOrEmpty(attribute?.Name))
            {
                return attribute.Name;
            }

            // If the view component didn't define a name explicitly then use the namespace + the
            // 'short name'.
            var shortName = GetShortNameByConvention(componentType);
            if (string.IsNullOrEmpty(componentType.Namespace))
            {
                return shortName;
            }
            else
            {
                return componentType.Namespace + "." + shortName;
            }
        }

        private static string GetShortNameByConvention(TypeInfo componentType)
        {
            if (componentType.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return componentType.Name.Substring(0, componentType.Name.Length - ViewComponentSuffix.Length);
            }
            else
            {
                return componentType.Name;
            }
        }

        public static bool IsComponent(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (!typeInfo.IsClass ||
                !typeInfo.IsPublic ||
                typeInfo.IsAbstract ||
                typeInfo.ContainsGenericParameters ||
                typeInfo.IsDefined(typeof(NonViewComponentAttribute)))
            {
                return false;
            }

            return
                typeInfo.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase) ||
                typeInfo.IsDefined(typeof(ViewComponentAttribute));
        }
    }
}
