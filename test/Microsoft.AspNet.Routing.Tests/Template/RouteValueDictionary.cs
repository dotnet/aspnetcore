
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    // This is just a placeholder
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
                foreach (var property in obj.GetType().GetTypeInfo().GetProperties())
                {
                    Add(property.Name, property.GetValue(obj));
                }
            }
        }
    }
}
