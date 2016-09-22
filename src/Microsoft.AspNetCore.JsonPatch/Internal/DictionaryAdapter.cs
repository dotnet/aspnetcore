// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class DictionaryAdapter : IAdapter
    {
        public bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            var dictionary = (IDictionary)target;
            
            // As per JsonPatch spec, if a key already exists, adding should replace the existing value
            dictionary[segment] = value;

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
            var dictionary = (IDictionary)target;

            value = dictionary[segment];
            errorMessage = null;
            return true;
        }

        public bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {
            var dictionary = (IDictionary)target;

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.Contains(segment))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            dictionary.Remove(segment);
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
            var dictionary = (IDictionary)target;

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.Contains(segment))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            dictionary[segment] = value;
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
            var dictionary = target as IDictionary;
            if (dictionary == null)
            {
                nextTarget = null;
                errorMessage = null;
                return false;
            }

            if (dictionary.Contains(segment))
            {
                nextTarget = dictionary[segment];
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
    }
}
