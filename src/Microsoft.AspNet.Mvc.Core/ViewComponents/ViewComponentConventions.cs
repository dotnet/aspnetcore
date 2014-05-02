// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
