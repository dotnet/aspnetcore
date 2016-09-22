// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ExpandoObjectAdapter : IAdapter
    {
        public bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var dictionary = (IDictionary<string, object>)target;

            var key = dictionary.GetKeyUsingCaseInsensitiveSearch(segment);

            // As per JsonPatch spec, if a key already exists, adding should replace the existing value
            dictionary[key] = ConvertValue(dictionary, key, value);

            errorMessage = null;
            return true;
        }

        public bool TryGet(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            var dictionary = (IDictionary<string, object>)target;

            var key = dictionary.GetKeyUsingCaseInsensitiveSearch(segment);
            value = dictionary[key];

            errorMessage = null;
            return true;
        }

        public bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {
            var dictionary = (IDictionary<string, object>)target;

            var key = dictionary.GetKeyUsingCaseInsensitiveSearch(segment);

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.ContainsKey(key))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            dictionary.Remove(key);

            errorMessage = null;
            return true;
        }

        public bool TryReplace(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var dictionary = (IDictionary<string, object>)target;

            var key = dictionary.GetKeyUsingCaseInsensitiveSearch(segment);

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.ContainsKey(key))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            dictionary[key] = ConvertValue(dictionary, key, value);

            errorMessage = null;
            return true;
        }

        public bool TryTraverse(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object nextTarget,
            out string errorMessage)
        {
            var expandoObject = target as ExpandoObject;
            if (expandoObject == null)
            {
                errorMessage = null;
                nextTarget = null;
                return false;
            }

            var dictionary = (IDictionary<string, object>)expandoObject;

            var key = dictionary.GetKeyUsingCaseInsensitiveSearch(segment);

            if (dictionary.ContainsKey(key))
            {
                nextTarget = dictionary[key];
                errorMessage = null;
                return true;
            }
            else
            {
                nextTarget = null;
                errorMessage = null;
                return false;
            }
        }

        private object ConvertValue(IDictionary<string, object> dictionary, string key, object newValue)
        {
            object existingValue = null;
            if (dictionary.TryGetValue(key, out existingValue))
            {
                if (existingValue != null)
                {
                    var conversionResult = ConversionResultProvider.ConvertTo(newValue, existingValue.GetType());
                    if (conversionResult.CanBeConverted)
                    {
                        return conversionResult.ConvertedInstance;
                    }
                }
            }
            return newValue;
        }
    }
}
