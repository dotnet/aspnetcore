
using System;
using System.Collections.Generic;
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
                foreach (var property in obj.GetType().GetTypeInfo().DeclaredProperties)
                {
                    var value = property.GetValue(obj);
                    Add(property.Name, value);
                }
            }
        }

        public RouteValueDictionary(IDictionary<string, object> other)
            : base(other, StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
