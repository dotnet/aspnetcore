// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class PocoAdapter : IAdapter
    {
        public bool TryAdd(
            object target,
            string segment,
            IContractResolver contractResolver,
            object value,
            out string errorMessage)
        {
            JsonProperty jsonProperty = null;
            if (!TryGetJsonProperty(target, contractResolver, segment, out jsonProperty))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            if (!jsonProperty.Writable)
            {
                errorMessage = Resources.FormatCannotUpdateProperty(segment);
                return false;
            }

            object convertedValue = null;
            if (!TryConvertValue(value, jsonProperty.PropertyType, out convertedValue))
            {
                errorMessage = Resources.FormatInvalidValueForProperty(value);
                return false;
            }

            jsonProperty.ValueProvider.SetValue(target, convertedValue);

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
            JsonProperty jsonProperty = null;
            if (!TryGetJsonProperty(target, contractResolver, segment, out jsonProperty))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                value = null;
                return false;
            }

            if (!jsonProperty.Readable)
            {
                errorMessage = Resources.FormatCannotReadProperty(segment);
                value = null;
                return false;
            }

            value = jsonProperty.ValueProvider.GetValue(target);
            errorMessage = null;
            return true;
        }

        public bool TryRemove(
            object target,
            string segment,
            IContractResolver contractResolver,
            out string errorMessage)
        {
            JsonProperty jsonProperty = null;
            if (!TryGetJsonProperty(target, contractResolver, segment, out jsonProperty))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            if (!jsonProperty.Writable)
            {
                errorMessage = Resources.FormatCannotUpdateProperty(segment);
                return false;
            }

            // Setting the value to "null" will use the default value in case of value types, and
            // null in case of reference types
            object value = null;
            if (jsonProperty.PropertyType.GetTypeInfo().IsValueType
                && Nullable.GetUnderlyingType(jsonProperty.PropertyType) == null)
            {
                value = Activator.CreateInstance(jsonProperty.PropertyType);
            }

            jsonProperty.ValueProvider.SetValue(target, value);

            errorMessage = null;
            return true;
        }

        public bool TryReplace(
            object target,
            string segment,
            IContractResolver
            contractResolver,
            object value,
            out string errorMessage)
        {
            JsonProperty jsonProperty = null;
            if (!TryGetJsonProperty(target, contractResolver, segment, out jsonProperty))
            {
                errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
                return false;
            }

            if (!jsonProperty.Writable)
            {
                errorMessage = Resources.FormatCannotUpdateProperty(segment);
                return false;
            }

            object convertedValue = null;
            if (!TryConvertValue(value, jsonProperty.PropertyType, out convertedValue))
            {
                errorMessage = Resources.FormatInvalidValueForProperty(value);
                return false;
            }

            jsonProperty.ValueProvider.SetValue(target, convertedValue);

            errorMessage = null;
            return true;
        }

        public bool TryTraverse(
            object target,
            string segment,
            IContractResolver contractResolver,
            out object value,
            out string errorMessage)
        {
            if (target == null)
            {
                value = null;
                errorMessage = null;
                return false;
            }

            JsonProperty jsonProperty = null;
            if (TryGetJsonProperty(target, contractResolver, segment, out jsonProperty))
            {
                value = jsonProperty.ValueProvider.GetValue(target);
                errorMessage = null;
                return true;
            }

            value = null;
            errorMessage = Resources.FormatTargetLocationAtPathSegmentNotFound(segment);
            return false;
        }

        private bool TryGetJsonProperty(
            object target,
            IContractResolver contractResolver,
            string segment,
            out JsonProperty jsonProperty)
        {
            var jsonObjectContract = contractResolver.ResolveContract(target.GetType()) as JsonObjectContract;
            if (jsonObjectContract != null)
            {
                var pocoProperty = jsonObjectContract
                    .Properties
                    .FirstOrDefault(p => string.Equals(p.PropertyName, segment, StringComparison.OrdinalIgnoreCase));

                if (pocoProperty != null)
                {
                    jsonProperty = pocoProperty;
                    return true;
                }
            }

            jsonProperty = null;
            return false;
        }

        private bool TryConvertValue(object value, Type propertyType, out object convertedValue)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(value, propertyType);
            if (!conversionResult.CanBeConverted)
            {
                convertedValue = null;
                return false;
            }

            convertedValue = conversionResult.ConvertedInstance;
            return true;
        }
    }
}
