// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public static class ViewComponentConventions
    {
        private const string ViewComponentSuffix = "ViewComponent";

        public static string GetComponentName([NotNull] TypeInfo componentType)
        {
            var attribute = componentType.GetCustomAttribute<ViewComponentAttribute>();
            if (attribute != null && !string.IsNullOrEmpty(attribute.Name))
            {
                return attribute.Name;
            }

            if (componentType.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return componentType.Name.Substring(0, componentType.Name.Length - ViewComponentSuffix.Length);
            }
            else
            {
                return componentType.Name;
            }
        }

        public static bool IsComponent([NotNull] TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass ||
                typeInfo.IsAbstract ||
                typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            return
                typeInfo.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase) ||
                typeInfo.GetCustomAttribute<ViewComponentAttribute>() != null;
        }
    }
}
