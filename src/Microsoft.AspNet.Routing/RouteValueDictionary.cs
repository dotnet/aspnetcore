// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Routing
{
    public class RouteValueDictionary : Dictionary<string, object>
    {
        public RouteValueDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public RouteValueDictionary(object obj)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            if (obj != null)
            {
                var type = obj.GetType();
                var allProperties = type.GetRuntimeProperties();
                
                // This is done to support 'new' properties that hide a property on a base class
                var orderedByDeclaringType = allProperties.OrderBy(p => p.DeclaringType == type ? 0 : 1);
                foreach (var property in orderedByDeclaringType)
                {
                    if (property.GetMethod != null && 
                        property.GetMethod.IsPublic &&
                        !property.GetMethod.IsStatic &&
                        property.GetIndexParameters().Length == 0)
                    {
                        var value = property.GetValue(obj);
                        if (ContainsKey(property.Name) && property.DeclaringType != type)
                        {
                            // This is a hidden property, ignore it.
                        }
                        else
                        {
                            Add(property.Name, value);
                        }
                    }
                }
            }
        }

        public RouteValueDictionary(IDictionary<string, object> other)
            : base(other, StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
