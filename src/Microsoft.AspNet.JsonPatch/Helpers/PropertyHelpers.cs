// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch.Helpers
{
    internal static class PropertyHelpers
    {
        public static JsonPatchProperty FindPropertyAndParent(
            object targetObject,
            string propertyPath,
            IContractResolver contractResolver)
        {
            try
            {
                var splitPath = propertyPath.Split('/');

                // skip the first one if it's empty
                var startIndex = (string.IsNullOrWhiteSpace(splitPath[0]) ? 1 : 0);

                for (int i = startIndex; i < splitPath.Length; i++)
                {
                    var jsonContract = (JsonObjectContract)contractResolver.ResolveContract(targetObject.GetType());

                    foreach (var property in jsonContract.Properties)
                    {
                        if (string.Equals(property.PropertyName, splitPath[i], StringComparison.OrdinalIgnoreCase))
                        {
                            if (i == (splitPath.Length - 1))
                            {
                                return new JsonPatchProperty(property, targetObject);
                            }
                            else
                            {
                                targetObject = property.ValueProvider.GetValue(targetObject);

                                // if property is of IList type then get the array index from splitPath and get the
                                // object at the indexed position from the list.
                                if (typeof(IList).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
                                {
                                    var index = int.Parse(splitPath[++i]);
                                    targetObject = ((IList)targetObject)[index];
                                }
                            }

                            break;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                // will result in JsonPatchException in calling class, as expected
                return null;
            }
        }

        internal static ConversionResult ConvertToActualType(Type propertyType, object value)
        {
            try
            {
                var o = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), propertyType);

                return new ConversionResult(true, o);
            }
            catch (Exception)
            {
                return new ConversionResult(false, null);
            }
        }

        internal static Type GetEnumerableType(Type type)
        {
            if (type == null) throw new ArgumentNullException();
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.GetTypeInfo().IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }
            return null;
        }

        internal static int GetNumericEnd(string path)
        {
            var possibleIndex = path.Substring(path.LastIndexOf("/") + 1);
            var castedIndex = -1;

            if (int.TryParse(possibleIndex, out castedIndex))
            {
                return castedIndex;
            }

            return -1;
        }
    }
}