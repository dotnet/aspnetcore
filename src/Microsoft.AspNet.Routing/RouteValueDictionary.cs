
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
                var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                // This is done to support 'new' properties that hide a property on a base class
                var orderedByDeclaringType = allProperties.OrderBy(p => p.DeclaringType == type ? 0 : 1);
                foreach (var property in orderedByDeclaringType)
                {
                    if (property.GetMethod != null && property.GetIndexParameters().Length == 0)
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
