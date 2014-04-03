using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal static class TypeHelper
    {
        /// <summary>
        /// Given an object, adds each instance property with a public get method as a key and its 
        /// associated value to a dictionary.
        /// </summary>
        //
        // The implementation of PropertyHelper will cache the property accessors per-type. This is
        // faster when the the same type is used multiple times with ObjectToDictionary.
        public static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (value != null)
            {
                foreach (var helper in PropertyHelper.GetProperties(value))
                {
                    dictionary.Add(helper.Name, helper.GetValue(value));
                }
            }

            return dictionary;
        }

    }
}
