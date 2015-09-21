// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.JsonPatch.Exceptions;
using Microsoft.AspNet.JsonPatch.Helpers;
using Microsoft.AspNet.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch.Adapters
{
    /// <inheritdoc />
    public class ObjectAdapter : IObjectAdapter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ObjectAdapter"/>.
        /// </summary>
        /// <param name="contractResolver">The <see cref="IContractResolver"/>.</param>
        /// <param name="logErrorAction">The <see cref="Action"/> for logging <see cref="JsonPatchError"/>.</param>
        public ObjectAdapter(
            IContractResolver contractResolver,
            Action<JsonPatchError> logErrorAction)
        {
            if (contractResolver == null)
            {
                throw new ArgumentNullException(nameof(contractResolver));
            }

            ContractResolver = contractResolver;
            LogErrorAction = logErrorAction;
        }

        /// <summary>
        /// Gets or sets the <see cref="IContractResolver"/>.
        /// </summary>
        public IContractResolver ContractResolver { get; }

        /// <summary>
        /// Action for logging <see cref="JsonPatchError"/>.
        /// </summary>
        public Action<JsonPatchError> LogErrorAction { get; }

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
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        public void Add(Operation operation, object objectToApplyTo)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            Add(operation.path, operation.value, objectToApplyTo, operation);
        }

        /// <summary>
        /// Add is used by various operations (eg: add, copy, ...), yet through different operations;
        /// This method allows code reuse yet reporting the correct operation on error
        /// </summary>
        private void Add(
            string path,
            object value,
            object objectToApplyTo,
            Operation operationToReport)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            if (operationToReport == null)
            {
                throw new ArgumentNullException(nameof(operationToReport));
            }

            // first up: if the path ends in a numeric value, we're inserting in a list and
            // that value represents the position; if the path ends in "-", we're appending
            // to the list.

            // get path result
            var pathResult = GetActualPropertyPath(
                path,
                objectToApplyTo,
                operationToReport);
            if (pathResult == null)
            {
                return;
            }

            var appendList = pathResult.ExecuteAtEnd;
            var positionAsInteger = pathResult.NumericEnd;
            var actualPathToProperty = pathResult.PathToProperty;

            var treeAnalysisResult = new ObjectTreeAnalysisResult(
                objectToApplyTo,
                actualPathToProperty,
                ContractResolver);

            if (!treeAnalysisResult.IsValidPathForAdd)
            {
                LogError(new JsonPatchError(
                    objectToApplyTo,
                    operationToReport,
                    Resources.FormatPropertyCannotBeAdded(path)));
                return;
            }

            if (treeAnalysisResult.UseDynamicLogic)
            {
                var container = treeAnalysisResult.Container;
                if (container.ContainsCaseInsensitiveKey(treeAnalysisResult.PropertyPathInParent))
                {
                    // Existing property.  
                    // If it's not an array, we need to check if the value fits the property type
                    // 
                    // If it's an array, we need to check if the value fits in that array type,
                    // and add it at the correct position (if allowed).
                    if (appendList || positionAsInteger > -1)
                    {
                        // get the actual type
                        var propertyValue = container.GetValueForCaseInsensitiveKey(treeAnalysisResult.PropertyPathInParent);
                        var typeOfPathProperty = propertyValue.GetType();

                        if (!IsNonStringArray(typeOfPathProperty))
                        {
                            LogError(new JsonPatchError(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));
                            return;
                        }

                        // now, get the generic type of the enumerable
                        var genericTypeOfArray = GetIListType(typeOfPathProperty);
                        var conversionResult = ConvertToActualType(genericTypeOfArray, value);
                        if (!conversionResult.CanBeConverted)
                        {
                            LogError(new JsonPatchError(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidValueForProperty(value, path)));
                            return;
                        }

                        // get value (it can be cast, we just checked that) 
                        var array = treeAnalysisResult.Container.GetValueForCaseInsensitiveKey(
                            treeAnalysisResult.PropertyPathInParent) as IList;

                        if (appendList)
                        {
                            array.Add(conversionResult.ConvertedInstance);
                            treeAnalysisResult.Container.SetValueForCaseInsensitiveKey(
                                treeAnalysisResult.PropertyPathInParent, array);
                        }
                        else
                        {
                            // specified index must not be greater than 
                            // the amount of items in the array
                            if (positionAsInteger > array.Count)
                            {
                                LogError(new JsonPatchError(
                                    objectToApplyTo,
                                    operationToReport,
                                    Resources.FormatInvalidIndexForArrayProperty(
                                        operationToReport.op,
                                        path)));
                                return;
                            }

                            array.Insert(positionAsInteger, conversionResult.ConvertedInstance);
                            treeAnalysisResult.Container.SetValueForCaseInsensitiveKey(
                                treeAnalysisResult.PropertyPathInParent, array);
                        }
                    }
                    else
                    {
                        // get the actual type
                        var typeOfPathProperty = treeAnalysisResult.Container
                            .GetValueForCaseInsensitiveKey(treeAnalysisResult.PropertyPathInParent).GetType();

                        // can the value be converted to the actual type?
                        var conversionResult = ConvertToActualType(typeOfPathProperty, value);
                        if (conversionResult.CanBeConverted)
                        {
                            treeAnalysisResult.Container.SetValueForCaseInsensitiveKey(
                                    treeAnalysisResult.PropertyPathInParent,
                                    conversionResult.ConvertedInstance);
                        }
                        else
                        {
                            LogError(new JsonPatchError(
                               objectToApplyTo,
                               operationToReport,
                               Resources.FormatInvalidValueForProperty(conversionResult.ConvertedInstance, path)));
                            return;
                        }
                    }
                }
                else
                {
                    // New property - add it.  
                    treeAnalysisResult.Container.Add(treeAnalysisResult.PropertyPathInParent, value);
                }
            }
            else
            {
                // If it's an array, add to that array.  If it's not, we replace.

                // is the path an array (but not a string (= char[]))?  In this case,
                // the path must end with "/position" or "/-", which we already determined before.

                var patchProperty = treeAnalysisResult.JsonPatchProperty;

                if (appendList || positionAsInteger > -1)
                {
                    if (!IsNonStringArray(patchProperty.Property.PropertyType))
                    {
                        LogError(new JsonPatchError(
                           objectToApplyTo,
                           operationToReport,
                           Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));
                        return;
                    }

                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);
                    var conversionResult = ConvertToActualType(genericTypeOfArray, value);
                    if (!conversionResult.CanBeConverted)
                    {
                        LogError(new JsonPatchError(
                              objectToApplyTo,
                              operationToReport,
                              Resources.FormatInvalidValueForProperty(conversionResult.ConvertedInstance, path)));
                        return;
                    }

                    if (!patchProperty.Property.Readable)
                    {
                        LogError(new JsonPatchError(
                                 objectToApplyTo,
                                 operationToReport,
                                 Resources.FormatCannotReadProperty(path)));
                        return;
                    }

                    var array = (IList)patchProperty.Property.ValueProvider.GetValue(patchProperty.Parent);
                    if (appendList)
                    {
                        array.Add(conversionResult.ConvertedInstance);
                    }
                    else if (positionAsInteger <= array.Count)
                    {
                        array.Insert(positionAsInteger, conversionResult.ConvertedInstance);
                    }
                    else
                    {
                        LogError(new JsonPatchError(
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

                    if (!conversionResultTuple.CanBeConverted)
                    {
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatInvalidValueForProperty(value, path)));
                        return;
                    }

                    if (!patchProperty.Property.Writable)
                    {
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatCannotUpdateProperty(path)));
                        return;
                    }

                    patchProperty.Property.ValueProvider.SetValue(
                        patchProperty.Parent,
                        conversionResultTuple.ConvertedInstance);
                }
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
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        public void Move(Operation operation, object objectToApplyTo)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            var valueAtFromLocationResult = GetValueAtLocation(operation.from, objectToApplyTo, operation);

            if (valueAtFromLocationResult.HasError)
            {
                // Error has already been logged in GetValueAtLocation.  We
                // must return, because remove / add should not be allowed to continue
                return;
            }

            // remove that value
            var removeResult = Remove(operation.from, objectToApplyTo, operation);

            if (removeResult.HasError)
            {
                // Return => error has already been logged in remove method.  We must
                // return, because add should not be allowed to continue
                return;
            }

            // add that value to the path location
            Add(operation.path,
                valueAtFromLocationResult.PropertyValue,
                objectToApplyTo,
                operation);
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
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        public void Remove(Operation operation, object objectToApplyTo)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            Remove(operation.path, objectToApplyTo, operation);
        }


        /// <summary>
        /// Remove is used by various operations (eg: remove, move, ...), yet through different operations;
        /// This method allows code reuse yet reporting the correct operation on error.  The return value
        /// contains the type of the item that has been removed (and a bool possibly signifying an error)
        /// This can be used by other methods, like replace, to ensure that we can pass in the correctly 
        /// typed value to whatever method follows.
        /// </summary>
        private RemovedPropertyTypeResult Remove(string path, object objectToApplyTo, Operation operationToReport)
        {
            // get path result
            var pathResult = GetActualPropertyPath(
                path,
                objectToApplyTo,
                operationToReport);

            if (pathResult == null)
            {
                return new RemovedPropertyTypeResult(null, true);
            }

            var removeFromList = pathResult.ExecuteAtEnd;
            var positionAsInteger = pathResult.NumericEnd;
            var actualPathToProperty = pathResult.PathToProperty;

            var treeAnalysisResult = new ObjectTreeAnalysisResult(
               objectToApplyTo,
               actualPathToProperty,
               ContractResolver);

            if (!treeAnalysisResult.IsValidPathForRemove)
            {
                LogError(new JsonPatchError(
                    objectToApplyTo,
                    operationToReport,
                    Resources.FormatPropertyCannotBeRemoved(path)));
                return new RemovedPropertyTypeResult(null, true);
            }

            if (treeAnalysisResult.UseDynamicLogic)
            {
                // if it's not an array, we can remove the property from
                // the dictionary.  If it's an array, we need to check the position first.
                if (removeFromList || positionAsInteger > -1)
                {
                    var propertyValue = treeAnalysisResult.Container
                        .GetValueForCaseInsensitiveKey(treeAnalysisResult.PropertyPathInParent);

                    // we cannot continue when the value is null, because to be able to
                    // continue we need to be able to check if the array is a non-string array
                    if (propertyValue == null)
                    {
                        LogError(new JsonPatchError(
                           objectToApplyTo,
                           operationToReport,
                           Resources.FormatCannotDeterminePropertyType(path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    var typeOfPathProperty = propertyValue.GetType();

                    if (!IsNonStringArray(typeOfPathProperty))
                    {
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    // now, get the generic type of the enumerable (we'll return this type)
                    var genericTypeOfArray = GetIListType(typeOfPathProperty);

                    // get the array
                    var array = (IList)treeAnalysisResult.Container.GetValueForCaseInsensitiveKey(
                        treeAnalysisResult.PropertyPathInParent);

                    if (array.Count == 0)
                    {
                        // if the array is empty, we should throw an error
                        LogError(new JsonPatchError(
                             objectToApplyTo,
                             operationToReport,
                             Resources.FormatInvalidIndexForArrayProperty(
                                 operationToReport.op,
                                 path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    if (removeFromList)
                    {
                        array.RemoveAt(array.Count - 1);
                        treeAnalysisResult.Container.SetValueForCaseInsensitiveKey(
                            treeAnalysisResult.PropertyPathInParent, array);

                        // return the type of the value that has been removed.
                        return new RemovedPropertyTypeResult(genericTypeOfArray, false);
                    }
                    else
                    {
                        if (positionAsInteger >= array.Count)
                        {
                            LogError(new JsonPatchError(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidIndexForArrayProperty(
                                    operationToReport.op,
                                    path)));
                            return new RemovedPropertyTypeResult(null, true);
                        }

                        array.RemoveAt(positionAsInteger);
                        treeAnalysisResult.Container.SetValueForCaseInsensitiveKey(
                            treeAnalysisResult.PropertyPathInParent, array);

                        // return the type of the value that has been removed.
                        return new RemovedPropertyTypeResult(genericTypeOfArray, false);
                    }
                }
                else
                {
                    // get the property
                    var getResult = treeAnalysisResult.Container.GetValueForCaseInsensitiveKey(
                        treeAnalysisResult.PropertyPathInParent);

                    // remove the property
                    treeAnalysisResult.Container.RemoveValueForCaseInsensitiveKey(
                        treeAnalysisResult.PropertyPathInParent);

                    // value is not null, we can determine the type
                    if (getResult != null)
                    {
                        var actualType = getResult.GetType();
                        return new RemovedPropertyTypeResult(actualType, false);
                    }
                    else
                    {
                        return new RemovedPropertyTypeResult(null, false);
                    }
                }
            }
            else
            {
                // not dynamic                  
                var patchProperty = treeAnalysisResult.JsonPatchProperty;

                if (removeFromList || positionAsInteger > -1)
                {
                    if (!IsNonStringArray(patchProperty.Property.PropertyType))
                    {
                        LogError(new JsonPatchError(
                               objectToApplyTo,
                               operationToReport,
                               Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    // now, get the generic type of the IList<> from Property type.
                    var genericTypeOfArray = GetIListType(patchProperty.Property.PropertyType);

                    if (!patchProperty.Property.Readable)
                    {
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatCannotReadProperty(path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    var array = (IList)patchProperty.Property.ValueProvider
                           .GetValue(patchProperty.Parent);

                    if (array.Count == 0)
                    {
                        // if the array is empty, we should throw an error
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatInvalidIndexForArrayProperty(
                                operationToReport.op,
                                path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    if (removeFromList)
                    {
                        array.RemoveAt(array.Count - 1);

                        // return the type of the value that has been removed
                        return new RemovedPropertyTypeResult(genericTypeOfArray, false);
                    }
                    else
                    {
                        if (positionAsInteger >= array.Count)
                        {
                            LogError(new JsonPatchError(
                                objectToApplyTo,
                                operationToReport,
                                Resources.FormatInvalidIndexForArrayProperty(
                                    operationToReport.op,
                                    path)));
                            return new RemovedPropertyTypeResult(null, true);
                        }

                        array.RemoveAt(positionAsInteger);

                        // return the type of the value that has been removed
                        return new RemovedPropertyTypeResult(genericTypeOfArray, false);
                    }
                }
                else
                {
                    if (!patchProperty.Property.Writable)
                    {
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatCannotUpdateProperty(path)));
                        return new RemovedPropertyTypeResult(null, true);
                    }

                    // setting the value to "null" will use the default value in case of value types, and
                    // null in case of reference types
                    object value = null;

                    if (patchProperty.Property.PropertyType.GetTypeInfo().IsValueType
                        && Nullable.GetUnderlyingType(patchProperty.Property.PropertyType) == null)
                    {
                        value = Activator.CreateInstance(patchProperty.Property.PropertyType);
                    }

                    patchProperty.Property.ValueProvider.SetValue(patchProperty.Parent, value);
                    return new RemovedPropertyTypeResult(patchProperty.Property.PropertyType, false);
                }
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
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        public void Replace(Operation operation, object objectToApplyTo)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            var removeResult = Remove(operation.path, objectToApplyTo, operation);

            if (removeResult.HasError)
            {
                // return => error has already been logged in remove method
                return;
            }

            if (!removeResult.HasError && removeResult.ActualType == null)
            {
                // the remove operation completed succesfully, but we could not determine the type.
                LogError(new JsonPatchError(
                                objectToApplyTo,
                                operation,
                                Resources.FormatCannotDeterminePropertyType(operation.from)));
                return;
            }

            var conversionResult = ConvertToActualType(removeResult.ActualType, operation.value);

            if (!conversionResult.CanBeConverted)
            {
                // invalid value for path
                LogError(new JsonPatchError(
                                objectToApplyTo,
                                operation,
                                Resources.FormatInvalidValueForProperty(operation.value, operation.path)));
                return;
            }

            Add(operation.path, conversionResult.ConvertedInstance, objectToApplyTo, operation);
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
        /// <param name="objectToApplyTo">Object to apply the operation to.</param>
        public void Copy(Operation operation, object objectToApplyTo)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            // get value at from location and add that value to the path location
            var valueAtFromLocationResult = GetValueAtLocation(operation.from, objectToApplyTo, operation);

            if (valueAtFromLocationResult.HasError)
            {
                // Return, error has already been logged in GetValueAtLocation
                return;
            }

            Add(operation.path,
                valueAtFromLocationResult.PropertyValue,
                objectToApplyTo,
                operation);
        }

        /// <summary>
        /// Method is used by Copy and Move to avoid duplicate code
        /// </summary>
        /// <param name="location">Location where value should be</param>
        /// <param name="objectToGetValueFrom">Object to inspect for the desired value</param>
        /// <param name="operationToReport">Operation to report in case of an error</param>
        /// <returns>GetValueResult containing value and a bool signifying a possible error</returns>
        private GetValueResult GetValueAtLocation(
            string location,
            object objectToGetValueFrom,
            Operation operationToReport)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (objectToGetValueFrom == null)
            {
                throw new ArgumentNullException(nameof(objectToGetValueFrom));
            }

            if (operationToReport == null)
            {
                throw new ArgumentNullException(nameof(operationToReport));
            }

            // get path result
            var pathResult = GetActualPropertyPath(
                location,
                objectToGetValueFrom,
                operationToReport);

            if (pathResult == null)
            {
                return new GetValueResult(null, true);
            }

            var getAtEndOfList = pathResult.ExecuteAtEnd;
            var positionAsInteger = pathResult.NumericEnd;
            var actualPathToProperty = pathResult.PathToProperty;

            var treeAnalysisResult = new ObjectTreeAnalysisResult(
               objectToGetValueFrom,
               actualPathToProperty,
               ContractResolver);

            if (treeAnalysisResult.UseDynamicLogic)
            {
                // if it's not an array, we can remove the property from
                // the dictionary.  If it's an array, we need to check the position first.
                if (getAtEndOfList || positionAsInteger > -1)
                {
                    var propertyValue = treeAnalysisResult.Container
                        .GetValueForCaseInsensitiveKey(treeAnalysisResult.PropertyPathInParent);

                    // we cannot continue when the value is null, because to be able to
                    // continue we need to be able to check if the array is a non-string array
                    if (propertyValue == null)
                    {
                        LogError(new JsonPatchError(
                           objectToGetValueFrom,
                           operationToReport,
                           Resources.FormatCannotDeterminePropertyType(location)));
                        return new GetValueResult(null, true);
                    }

                    var typeOfPathProperty = propertyValue.GetType();

                    if (!IsNonStringArray(typeOfPathProperty))
                    {
                        LogError(new JsonPatchError(
                            objectToGetValueFrom,
                            operationToReport,
                            Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, location)));
                        return new GetValueResult(null, true);
                    }

                    // get the array
                    var array = (IList)treeAnalysisResult.Container.GetValueForCaseInsensitiveKey(
                        treeAnalysisResult.PropertyPathInParent);

                    if (positionAsInteger >= array.Count)
                    {
                        LogError(new JsonPatchError(
                            objectToGetValueFrom,
                            operationToReport,
                            Resources.FormatInvalidIndexForArrayProperty(
                                operationToReport.op,
                                location)));
                        return new GetValueResult(null, true);
                    }

                    if (getAtEndOfList)
                    {
                        return new GetValueResult(array[array.Count - 1], false);
                    }
                    else
                    {
                        return new GetValueResult(array[positionAsInteger], false);
                    }
                }
                else
                {
                    // get the property
                    var propertyValueAtLocation = treeAnalysisResult.Container.GetValueForCaseInsensitiveKey(
                        treeAnalysisResult.PropertyPathInParent);

                    return new GetValueResult(propertyValueAtLocation, false);
                }
            }
            else
            {
                // not dynamic                  
                var patchProperty = treeAnalysisResult.JsonPatchProperty;

                if (getAtEndOfList || positionAsInteger > -1)
                {
                    if (!IsNonStringArray(patchProperty.Property.PropertyType))
                    {
                        LogError(new JsonPatchError(
                               objectToGetValueFrom,
                               operationToReport,
                               Resources.FormatInvalidIndexForArrayProperty(operationToReport.op, location)));
                        return new GetValueResult(null, true);
                    }

                    if (!patchProperty.Property.Readable)
                    {
                        LogError(new JsonPatchError(
                            objectToGetValueFrom,
                            operationToReport,
                            Resources.FormatCannotReadProperty(location)));
                        return new GetValueResult(null, true);
                    }

                    var array = (IList)patchProperty.Property.ValueProvider
                           .GetValue(patchProperty.Parent);

                    if (positionAsInteger >= array.Count)
                    {
                        LogError(new JsonPatchError(
                            objectToGetValueFrom,
                            operationToReport,
                            Resources.FormatInvalidIndexForArrayProperty(
                                operationToReport.op,
                                location)));
                        return new GetValueResult(null, true);
                    }

                    if (getAtEndOfList)
                    {
                        return new GetValueResult(array[array.Count - 1], false);
                    }
                    else
                    {
                        return new GetValueResult(array[positionAsInteger], false);
                    }
                }
                else
                {
                    if (!patchProperty.Property.Readable)
                    {
                        LogError(new JsonPatchError(
                            objectToGetValueFrom,
                            operationToReport,
                            Resources.FormatCannotReadProperty(
                                location)));
                        return new GetValueResult(null, true);
                    }

                    var propertyValueAtLocation = patchProperty.Property.ValueProvider
                                 .GetValue(patchProperty.Parent);

                    return new GetValueResult(propertyValueAtLocation, false);
                }
            }
        }

        private bool IsNonStringArray(Type type)
        {
            if (GetIListType(type) != null)
            {
                return true;
            }

            return (!(type == typeof(string)) && typeof(IList).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
        }

        private void LogError(JsonPatchError jsonPatchError)
        {
            if (LogErrorAction != null)
            {
                LogErrorAction(jsonPatchError);
            }
            else
            {
                throw new JsonPatchException(jsonPatchError);
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

        private Type GetIListType(Type type)
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

        private bool IsGenericListType(Type type)
        {
            if (type.GetTypeInfo().IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return true;
            }

            return false;
        }

        private ActualPropertyPathResult GetActualPropertyPath(
            string propertyPath,
            object objectToApplyTo,
            Operation operationToReport)
        {
            if (propertyPath == null)
            {
                throw new ArgumentNullException(nameof(propertyPath));
            }

            if (objectToApplyTo == null)
            {
                throw new ArgumentNullException(nameof(objectToApplyTo));
            }

            if (operationToReport == null)
            {
                throw new ArgumentNullException(nameof(operationToReport));
            }

            if (propertyPath.EndsWith("/-"))
            {
                return new ActualPropertyPathResult(-1, propertyPath.Substring(0, propertyPath.Length - 2), true);
            }
            else
            {
                var possibleIndex = propertyPath.Substring(propertyPath.LastIndexOf("/") + 1);
                int castedIndex = -1;
                if (int.TryParse(possibleIndex, out castedIndex))
                {
                    // has numeric end.  
                    if (castedIndex > -1)
                    {
                        var pathToProperty = propertyPath.Substring(
                           0,
                           propertyPath.LastIndexOf('/' + castedIndex.ToString()));

                        return new ActualPropertyPathResult(castedIndex, pathToProperty, false);
                    }
                    else
                    {
                        // negative position - invalid path
                        LogError(new JsonPatchError(
                            objectToApplyTo,
                            operationToReport,
                            Resources.FormatNegativeIndexForArrayProperty(operationToReport.op, propertyPath)));
                        return null;
                    }
                }

                return new ActualPropertyPathResult(-1, propertyPath, false);
            }
        }
    }
}