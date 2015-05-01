// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.JsonPatch.Exceptions;
using Microsoft.AspNet.JsonPatch.Helpers;
using Microsoft.AspNet.JsonPatch.Operations;
using Microsoft.Framework.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch.Adapters
{
    /// <inheritdoc />
    public class ObjectAdapter<TModel> : IObjectAdapter<TModel> where TModel : class
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ObjectAdapter{TModel}"/>.
        /// </summary>
        /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
        /// <param name="logErrorAction">The <see cref="Action"/> for logging <see cref="JsonPatchError{TModel}"/>.</param>
        public ObjectAdapter(IContractResolver contractResolver, Action<JsonPatchError<TModel>> logErrorAction)
        {
            ContractResolver = contractResolver;
            LogErrorAction = logErrorAction;
        }

        /// <summary>
        /// Gets or sets the <see cref="IContractResolver"/>.
        /// </summary>
        public IContractResolver ContractResolver { get; }

        /// <summary>
        /// Action for logging <see cref="JsonPatchError{TModel}"/>.
        /// </summary>
        public Action<JsonPatchError<TModel>> LogErrorAction { get; }

        /// <summary>
        /// The "add" operation performs one of the following functions,
        /// depending upon what the target location references:
        ///
        /// o  If the target location specifies an array index, a new value is
        ///    inserted into the array at the specified index.
        ///
        /// o  If the target location specifies an object member that does not
        ///    already exist, a new member is added to the object.
        ///
        /// o  If the target location specifies an object member that does exist,
        ///    that member's value is replaced.
        ///
        /// The operation object MUST contain a "value" member whose content
        /// specifies the value to be added.
        ///
        /// For example:
        ///
        /// { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
        ///
        /// When the operation is applied, the target location MUST reference one
        /// of:
        ///
        /// o  The root of the target document - whereupon the specified value
        ///    becomes the entire content of the target document.
        ///
        /// o  A member to add to an existing object - whereupon the supplied
        ///    value is added to that object at the indicated location.  If the
        ///    member already exists, it is replaced by the specified value.
        ///
        /// o  An element to add to an existing array - whereupon the supplied
        ///    value is added to the array at the indicated location.  Any
        ///    elements at or above the specified index are shifted one position
        ///    to the right.  The specified index MUST NOT be greater than the
        ///    number of elements in the array.  If the "-" character is used to
        ///    index the end of the array (see [RFC6901]), this has the effect of
        ///    appending the value to the array.
        ///
        /// Because this operation is designed to add to existing objects and
        /// arrays, its target location will often not exist.  Although the
        /// pointer's error handling algorithm will thus be invoked, this
        /// specification defines the error handling behavior for "add" pointers
        /// to ignore that error and add the value as specified.
        ///
        /// However, the object itself or an array containing it does need to
        /// exist, and it remains an error for that not to be the case.  For
        /// example, an "add" with a target location of "/a/b" starting with this
        /// document:
        ///
        /// { "a": { "foo": 1 } }
        ///
        /// is not an error, because "a" exists, and "b" will be added to its
        /// value.  It is an error in this document:
        ///
        /// { "q": { "bar": 2 } }
        ///
        /// because "a" does not exist.
        /// </summary>
        /// <param name="operation">The add operation.</param>
        /// <param name="objectApplyTo">Object to apply the operation to.</param>
        public void Add(Operation<TModel> operation, TModel objectToApplyTo)
        {
            Add(operation.path, operation.value, objectToApplyTo, operation);
        }

        /// <summary>
        /// Add is used by various operations (eg: add, copy, ...), yet through different operations;
        /// This method allows code reuse yet reporting the correct operation on error
        /// </summary>
        private void Add(string path, object value, TModel objectToApplyTo, Operation<TModel> operationToReport)
        {
            // add, in this implementation, does not just "add" properties - that's
            // technically impossible;  It can however be used to add items to arrays,
            // or to replace values.

            // first up: if the path ends in a numeric value, we're inserting in a list and
            // that value represents the position; if the path ends in "-", we're appending
            // to the list.
            var appendList = false;
            var positionAsInteger = -1;
            var actualPathToProperty = path;

            if (path.EndsWith("/-"))
            {
                appendList = true;
                actualPathToProperty = path.Substring(0, path.Length - 2);
            }
            else
            {
                positionAsInteger = GetNumericEnd(path);

                if (positionAsInteger > -1)
                {
                    actualPathToProperty = path.Substring(0,
                        path.LastIndexOf('/' + positionAsInteger.ToString()));
                }
            }

            var patchProperty = FindPropertyAndParent(objectToApplyTo, actualPathToProperty);

            // does property at path exist?
            if (!CheckIfPropertyExists(patchProperty, objectToApplyTo, operationToReport, path))
            {
                return;
            }

            // it exists.  If it' an array, add to that array.  If it's not, we replace.
            // is the path an array (but not a string (= char[]))?  In this case,
            // the path must end with "/position" or "/-", which we already determined before.
            if (appendList || positionAsInteger > -1)
            {
                // what if it's an array but there's no position??
                if (IsNonStringArray(patchProperty.Property.PropertyType))
                {
                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);

                    var conversionResult = ConvertToActualType(genericTypeOfArray, value);

                    if (!CheckIfPropertyCanBeSet(conversionResult, objectToApplyTo, operationToReport, path))
                    {
                        return;
                    }

                    // get value (it can be cast, we just checked that)
                    var array = (IList)patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);

                    if (appendList)
                    {
                        array.Add(conversionResult.ConvertedInstance);
                    }
                    else
                    {
                        // specified index must not be greater than the amount of items in the array
                        if (positionAsInteger <= array.Count)
                        {
                            array.Insert(positionAsInteger, conversionResult.ConvertedInstance);
                        }
                        else
                        {
                            LogError(new JsonPatchError<TModel>(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));

                            return;
                        }
                    }
                }
                else
                {
                    LogError(new JsonPatchError<TModel>(
                        objectToApplyTo,
                        operationToReport,
                        Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));

                    return;
                }
            }
            else
            {
                var conversionResultTuple = ConvertToActualType(
                    patchProperty.Property.PropertyType,
                    value);

                // Is conversion successful
                if (!CheckIfPropertyCanBeSet(conversionResultTuple, objectToApplyTo, operationToReport, path))
                {
                    return;
                }

                patchProperty.Property.ValueProvider.SetValue(
                        patchProperty.Parent,
                        conversionResultTuple.ConvertedInstance);
            }
        }

        /// <summary>
        /// The "move" operation removes the value at a specified location and
        /// adds it to the target location.
        ///
        /// The operation object MUST contain a "from" member, which is a string
        /// containing a JSON Pointer value that references the location in the
        /// target document to move the value from.
        ///
        /// The "from" location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        ///
        /// This operation is functionally identical to a "remove" operation on
        /// the "from" location, followed immediately by an "add" operation at
        /// the target location with the value that was just removed.
        ///
        /// The "from" location MUST NOT be a proper prefix of the "path"
        /// location; i.e., a location cannot be moved into one of its children.
        /// </summary>
        /// <param name="operation">The move operation.</param>
        /// <param name="objectApplyTo">Object to apply the operation to.</param>
        public void Move(Operation<TModel> operation, TModel objectToApplyTo)
        {
            // get value at from location
            object valueAtFromLocation = null;
            var positionAsInteger = -1;
            var actualFromProperty = operation.from;

            positionAsInteger = GetNumericEnd(operation.from);

            if (positionAsInteger > -1)
            {
                actualFromProperty = operation.from.Substring(0,
                    operation.from.IndexOf('/' + positionAsInteger.ToString()));
            }

            var patchProperty = FindPropertyAndParent(objectToApplyTo, actualFromProperty);

            // does property at from exist?
            if (!CheckIfPropertyExists(patchProperty, objectToApplyTo, operation, operation.from))
            {
                return;
            }

            // is the path an array (but not a string (= char[]))?  In this case,
            // the path must end with "/position" or "/-", which we already determined before.
            if (positionAsInteger > -1)
            {
                if (IsNonStringArray(patchProperty.Property.PropertyType))
                {
                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);

                    // get value (it can be cast, we just checked that)
                    var array = (IList)patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);

                    if (array.Count <= positionAsInteger)
                    {
                        LogError(new JsonPatchError<TModel>(
                            objectToApplyTo,
                            operation,
                            Resources.FormatInvalidIndexForArrayProperty(operation.op, operation.from)));

                        return;
                    }

                    valueAtFromLocation = array[positionAsInteger];
                }
                else
                {
                    LogError(new JsonPatchError<TModel>(
                        objectToApplyTo,
                        operation,
                        Resources.FormatInvalidPathForArrayProperty(operation.op, operation.from)));

                    return;
                }
            }
            else
            {
                // no list, just get the value
                // set the new value
                valueAtFromLocation = patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);
            }

            // remove that value
            Remove(operation.from, objectToApplyTo, operation);

            // add that value to the path location
            Add(operation.path, valueAtFromLocation, objectToApplyTo, operation);
        }

        /// <summary>
        /// The "remove" operation removes the value at the target location.
        ///
        /// The target location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "remove", "path": "/a/b/c" }
        ///
        /// If removing an element from an array, any elements above the
        /// specified index are shifted one position to the left.
        /// </summary>
        /// <param name="operation">The remove operation.</param>
        /// <param name="objectApplyTo">Object to apply the operation to.</param>
        public void Remove(Operation<TModel> operation, TModel objectToApplyTo)
        {
            Remove(operation.path, objectToApplyTo, operation);
        }

        /// <summary>
        /// Remove is used by various operations (eg: remove, move, ...), yet through different operations;
        /// This method allows code reuse yet reporting the correct operation on error
        /// </summary>
        private void Remove(string path, TModel objectToApplyTo, Operation<TModel> operationToReport)
        {
            var removeFromList = false;
            var positionAsInteger = -1;
            var actualPathToProperty = path;

            if (path.EndsWith("/-"))
            {
                removeFromList = true;
                actualPathToProperty = path.Substring(0, path.Length - 2);
            }
            else
            {
                positionAsInteger = GetNumericEnd(path);

                if (positionAsInteger > -1)
                {
                    actualPathToProperty = path.Substring(0,
                        path.IndexOf('/' + positionAsInteger.ToString()));
                }
            }

            var patchProperty = FindPropertyAndParent(objectToApplyTo, actualPathToProperty);

            // does the target location exist?
            if (!CheckIfPropertyExists(patchProperty, objectToApplyTo, operationToReport, path))
            {
                return;
            }

            // get the property, and remove it - in this case, for DTO's, that means setting
            // it to null or its default value; in case of an array, remove at provided index
            // or at the end.
            if (removeFromList || positionAsInteger > -1)
            {
                // what if it's an array but there's no position??
                if (IsNonStringArray(patchProperty.Property.PropertyType))
                {
                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);

                    // get value (it can be cast, we just checked that)
                    var array = (IList)patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);

                    if (removeFromList)
                    {
                        array.RemoveAt(array.Count - 1);
                    }
                    else
                    {
                        if (positionAsInteger < array.Count)
                        {
                            array.RemoveAt(positionAsInteger);
                        }
                        else
                        {
                            LogError(new JsonPatchError<TModel>(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));

                            return;
                        }
                    }
                }
                else
                {
                    LogError(new JsonPatchError<TModel>(
                        objectToApplyTo,
                        operationToReport,
                        Resources.FormatInvalidPathForArrayProperty(operationToReport.op, path)));

                    return;
                }
            }
            else
            {
                // setting the value to "null" will use the default value in case of value types, and
                // null in case of reference types
                object value = null;

                if (patchProperty.Property.PropertyType.GetTypeInfo().IsValueType
                    && Nullable.GetUnderlyingType(patchProperty.Property.PropertyType) == null)
                {
                    value = Activator.CreateInstance(patchProperty.Property.PropertyType);
                }

                patchProperty.Property.ValueProvider.SetValue(patchProperty.Parent, value);
            }
        }

        /// <summary>
        /// The "replace" operation replaces the value at the target location
        /// with a new value.  The operation object MUST contain a "value" member
        /// whose content specifies the replacement value.
        ///
        /// The target location MUST exist for the operation to be successful.
        ///
        /// For example:
        ///
        /// { "op": "replace", "path": "/a/b/c", "value": 42 }
        ///
        /// This operation is functionally identical to a "remove" operation for
        /// a value, followed immediately by an "add" operation at the same
        /// location with the replacement value.
        ///
        /// Note: even though it's the same functionally, we do not call remove + add
        /// for performance reasons (multiple checks of same requirements).
        /// </summary>
        /// <param name="operation">The replace operation.</param>
        /// <param name="objectApplyTo">Object to apply the operation to.</param>
        public void Replace(Operation<TModel> operation, TModel objectToApplyTo)
        {
            Remove(operation.path, objectToApplyTo, operation);
            Add(operation.path, operation.value, objectToApplyTo, operation);
        }

        /// <summary>
        ///  The "copy" operation copies the value at a specified location to the
        ///  target location.
        ///
        ///  The operation object MUST contain a "from" member, which is a string
        ///  containing a JSON Pointer value that references the location in the
        ///  target document to copy the value from.
        ///
        ///  The "from" location MUST exist for the operation to be successful.
        ///
        ///  For example:
        ///
        ///  { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        ///
        ///  This operation is functionally identical to an "add" operation at the
        ///  target location using the value specified in the "from" member.
        ///
        ///  Note: even though it's the same functionally, we do not call add with
        ///  the value specified in from for performance reasons (multiple checks of same requirements).
        /// </summary>
        /// <param name="operation">The copy operation.</param>
        /// <param name="objectApplyTo">Object to apply the operation to.</param>
        public void Copy(Operation<TModel> operation, TModel objectToApplyTo)
        {
            // get value at from location
            object valueAtFromLocation = null;
            var positionAsInteger = -1;
            var actualFromProperty = operation.from;

            positionAsInteger = GetNumericEnd(operation.from);

            if (positionAsInteger > -1)
            {
                actualFromProperty = operation.from.Substring(0,
                    operation.from.IndexOf('/' + positionAsInteger.ToString()));
            }

            var patchProperty = FindPropertyAndParent(objectToApplyTo, actualFromProperty);

            // does property at from exist?
            if (!CheckIfPropertyExists(patchProperty, objectToApplyTo, operation, operation.from))
            {
                return;
            }

            // get the property path
            // is the path an array (but not a string (= char[]))?  In this case,
            // the path must end with "/position" or "/-", which we already determined before.
            if (positionAsInteger > -1)
            {
                if (IsNonStringArray(patchProperty.Property.PropertyType))
                {
                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);

                    // get value (it can be cast, we just checked that)
                    var array = (IList)patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);

                    if (array.Count <= positionAsInteger)
                    {
                        LogError(new JsonPatchError<TModel>(
                            objectToApplyTo,
                            operation,
                            Resources.FormatInvalidIndexForArrayProperty(operation.op, operation.from)));

                        return;
                    }

                    valueAtFromLocation = array[positionAsInteger];
                }
                else
                {
                    LogError(new JsonPatchError<TModel>(
                        objectToApplyTo,
                        operation,
                        Resources.FormatInvalidPathForArrayProperty(operation.op, operation.from)));

                    return;
                }
            }
            else
            {
                // no list, just get the value
                // set the new value
                valueAtFromLocation = patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);
            }

            // add operation to target location with that value.
            Add(operation.path, valueAtFromLocation, objectToApplyTo, operation);
        }

        private bool CheckIfPropertyExists(
            JsonPatchProperty patchProperty,
            TModel objectToApplyTo,
            Operation<TModel> operation,
            string propertyPath)
        {
            if (patchProperty == null)
            {
                LogError(new JsonPatchError<TModel>(
                    objectToApplyTo,
                    operation,
                    Resources.FormatPropertyDoesNotExist(propertyPath)));

                return false;
            }

            if (patchProperty.Property.Ignored)
            {
                LogError(new JsonPatchError<TModel>(
                    objectToApplyTo,
                    operation,
                    Resources.FormatCannotUpdateProperty(propertyPath)));

                return false;
            }

            return true;
        }

        private bool IsNonStringArray(Type type)
        {
            if (GetIListType(type) != null)
            {
                return true;
            }

            return (!(type == typeof(string)) && typeof(IList).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
        }

        private bool CheckIfPropertyCanBeSet(
            ConversionResult result,
            TModel objectToApplyTo,
            Operation<TModel> operation,
            string path)
        {
            if (!result.CanBeConverted)
            {
                LogError(new JsonPatchError<TModel>(
                    objectToApplyTo,
                    operation,
                    Resources.FormatInvalidValueForProperty(result.ConvertedInstance, path)));

                return false;
            }

            return true;
        }

        private void LogError(JsonPatchError<TModel> jsonPatchError)
        {
            if (LogErrorAction != null)
            {
                LogErrorAction(jsonPatchError);
            }
            else
            {
                throw new JsonPatchException<TModel>(jsonPatchError);
            }
        }

        private JsonPatchProperty FindPropertyAndParent(
            object targetObject,
            string propertyPath)
        {
            try
            {
                var splitPath = propertyPath.Split('/');

                // skip the first one if it's empty
                var startIndex = (string.IsNullOrWhiteSpace(splitPath[0]) ? 1 : 0);

                for (int i = startIndex; i < splitPath.Length; i++)
                {
                    var jsonContract = (JsonObjectContract)ContractResolver.ResolveContract(targetObject.GetType());

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
                               if (GetIListType(property.PropertyType) != null)
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

        private ConversionResult ConvertToActualType(Type propertyType, object value)
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

        private Type GetIListType([NotNull] Type type)
        {
            if (IsGenericListType(type))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (Type interfaceType in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (IsGenericListType(interfaceType))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private bool IsGenericListType([NotNull] Type type)
        {
            if (type.GetTypeInfo().IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return true;
            }

            return false;
        }

        private int GetNumericEnd(string path)
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