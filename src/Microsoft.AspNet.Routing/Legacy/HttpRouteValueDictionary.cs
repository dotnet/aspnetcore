// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Routing.Legacy
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "This class will never be serialized.")]
    public class HttpRouteValueDictionary : Dictionary<string, object>
    {
        public HttpRouteValueDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public HttpRouteValueDictionary(IDictionary<string, object> dictionary)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, object> current in dictionary)
                {
                    Add(current.Key, current.Value);
                }
            }
        }

        public HttpRouteValueDictionary(object values)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            IDictionary<string, object> valuesAsDictionary = values as IDictionary<string, object>;
            if (valuesAsDictionary != null)
            {
                foreach (KeyValuePair<string, object> current in valuesAsDictionary)
                {
                    Add(current.Key, current.Value);
                }
            }
            else if (values != null)
            {
                foreach (PropertyHelper property in PropertyHelper.GetProperties(values))
                {
                    // Extract the property values from the property helper
                    // The advantage here is that the property helper caches fast accessors.
                    Add(property.Name, property.GetValue(values));
                }
            }
        }
    }
}
#endif
