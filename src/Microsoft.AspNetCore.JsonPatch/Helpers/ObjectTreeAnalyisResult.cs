// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Helpers
{
    internal class ObjectTreeAnalysisResult
    {
        // either the property is part of the container dictionary,
        // or we have a direct reference to a JsonPatchProperty instance

        public bool UseDynamicLogic { get; private set; }

        public bool IsValidPathForAdd { get; private set; }

        public bool IsValidPathForRemove { get; private set; }

        public IDictionary<string, object> Container { get; private set; }

        public string PropertyPathInParent { get; private set; }

        public JsonPatchProperty JsonPatchProperty { get; private set; }

        public ObjectTreeAnalysisResult(
            object objectToSearch,
            string propertyPath,
            IContractResolver contractResolver)
        {
            // construct the analysis result.

            // split the propertypath, and if necessary, remove the first 
            // empty item (that's the case when it starts with a "/")
            var propertyPathTree = propertyPath.Split(
                new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);

            // we've now got a split up property tree "base/property/otherproperty/..."
            int lastPosition = 0;
            object targetObject = objectToSearch;
            for (int i = 0; i < propertyPathTree.Length; i++)
            {
                lastPosition = i;

                // if the current target object is an ExpandoObject (IDictionary<string, object>),
                // we cannot use the ContractResolver.
                var dictionary = targetObject as IDictionary<string, object>;
                if (dictionary != null)
                {
                    // find the value in the dictionary                   
                    if (dictionary.ContainsCaseInsensitiveKey(propertyPathTree[i]))
                    {
                        var possibleNewTargetObject = dictionary.GetValueForCaseInsensitiveKey(propertyPathTree[i]);

                        // unless we're at the last item, we should set the targetobject
                        // to the new object.  If we're at the last item, we need to stop
                        if (i != propertyPathTree.Length - 1)
                        {
                            targetObject = possibleNewTargetObject;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // if the current part of the path is numeric, this means we're trying
                    // to get the propertyInfo of a specific object in an array.  To allow
                    // for this, the previous value (targetObject) must be an IEnumerable, and
                    // the position must exist.

                    int numericValue = -1;
                    if (int.TryParse(propertyPathTree[i], out numericValue))
                    {
                        var element = GetElementAtFromObject(targetObject, numericValue);
                        if (element != null)
                        {
                            targetObject = element;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        var jsonContract = (JsonObjectContract)contractResolver.ResolveContract(targetObject.GetType());

                        // does the property exist?
                        var attemptedProperty = jsonContract
                            .Properties
                            .FirstOrDefault(p => string.Equals(p.PropertyName, propertyPathTree[i], StringComparison.OrdinalIgnoreCase));

                        if (attemptedProperty != null)
                        {
                            // unless we're at the last item, we should continue searching.
                            // If we're at the last item, we need to stop
                            if ((i != propertyPathTree.Length - 1))
                            {
                                targetObject = attemptedProperty.ValueProvider.GetValue(targetObject);
                            }
                        }
                        else
                        {
                            // property cannot be found, and we're not working with dynamics.  
                            // Stop, and return invalid path.
                            break;
                        }
                    }
                }
            }

            if (propertyPathTree.Length - lastPosition != 1)
            {
                IsValidPathForAdd = false;
                IsValidPathForRemove = false;
                return;
            }

            // two things can happen now.  The targetproperty can be an IDictionary - in that
            // case, it's valid for add if there's 1 item left in the propertyPathTree.
            //
            // it can also be a property info.  In that case, if there's nothing left in the path
            // tree we're at the end, if there's one left we can try and set that. 
            if (targetObject is IDictionary<string, object>)
            {
                UseDynamicLogic = true;

                Container = (IDictionary<string, object>)targetObject;
                IsValidPathForAdd = true;
                PropertyPathInParent = propertyPathTree[propertyPathTree.Length - 1];

                // to be able to remove this property, it must exist
                IsValidPathForRemove = Container.ContainsCaseInsensitiveKey(PropertyPathInParent);
            }
            else if (targetObject is IList)
            {
                System.Diagnostics.Debugger.Launch();
                UseDynamicLogic = false;

                int index;
                if (!Int32.TryParse(propertyPathTree[propertyPathTree.Length - 1], out index))
                {
                    // We only support indexing into a list
                    IsValidPathForAdd = false;
                    IsValidPathForRemove = false;
                    return;
                }

                IsValidPathForAdd = true;
                IsValidPathForRemove = ((IList)targetObject).Count > index;
                PropertyPathInParent = propertyPathTree[propertyPathTree.Length - 1];
            }
            else
            {
                UseDynamicLogic = false;

                var property = propertyPathTree[propertyPathTree.Length - 1];
                var jsonContract = (JsonObjectContract)contractResolver.ResolveContract(targetObject.GetType());
                var attemptedProperty = jsonContract
                    .Properties
                    .FirstOrDefault(p => string.Equals(p.PropertyName, property, StringComparison.OrdinalIgnoreCase));

                if (attemptedProperty == null)
                {
                    IsValidPathForAdd = false;
                    IsValidPathForRemove = false;
                }
                else
                {
                    IsValidPathForAdd = true;
                    IsValidPathForRemove = true;
                    JsonPatchProperty = new JsonPatchProperty(attemptedProperty, targetObject);
                    PropertyPathInParent = property;
                }
            }
        }

        private object GetElementAtFromObject(object targetObject, int numericValue)
        {
            if (numericValue > -1)
            {
                // Check if the targetobject is an IEnumerable,
                // and if the position is valid.
                if (targetObject is IEnumerable)
                {
                    var indexable = ((IEnumerable)targetObject).Cast<object>();

                    if (indexable.Count() >= numericValue)
                    {
                        return indexable.ElementAt(numericValue);
                    }
                    else { return null; }
                }
                else { return null; }
            }
            else
            {
                return null;
            }
        }

    }
}
