// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class ListAdapter : IAdapter
{
    #region Existing implementation
    public virtual bool TryAdd(object target, string segment, JsonSerializerOptions jsonSerializerOptions, object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var targetCollectionCount = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(targetCollectionCount, segment, OperationType.Add, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        if (positionInfo.Type == PositionType.EndOfList)
        {
            GenericListOrJsonArrayUtilities.AddElement(target, convertedValue);
        }
        else
        {
            GenericListOrJsonArrayUtilities.InsertElementAt(target, positionInfo.Index, convertedValue);
        }

        errorMessage = null;
        return true;
    }
    #endregion

    public virtual bool TryGet(object target, string segment, JsonSerializerOptions jsonSerializerOptions, out object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out _, out errorMessage))
        {
            value = null;
            return false;
        }

        var targetCollectionCount = GenericListOrJsonArrayUtilities.GetCount(target);

        if (!TryGetPositionInfo(targetCollectionCount, segment, OperationType.Get, out var positionInfo, out errorMessage))
        {
            value = null;
            return false;
        }

        var valueIndex = positionInfo.Type == PositionType.EndOfList ? targetCollectionCount - 1 : positionInfo.Index;
        value = GenericListOrJsonArrayUtilities.GetElementAt(target, valueIndex);

        errorMessage = null;
        return true;
    }

    public virtual bool TryRemove(object target, string segment, JsonSerializerOptions jsonSerializerOptions, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out _, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(count, segment, OperationType.Remove, out var positionInfo, out errorMessage))
        {
            return false;
        }

        var indexToRemove = positionInfo.Type == PositionType.EndOfList ? count - 1 : positionInfo.Index;
        GenericListOrJsonArrayUtilities.RemoveElementAt(target, indexToRemove);

        errorMessage = null;
        return true;
    }

    public virtual bool TryReplace(object target, string segment, JsonSerializerOptions jsonSerializerOptions, object value, out string errorMessage)
    {
        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);
        if (!TryGetPositionInfo(count, segment, OperationType.Replace, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        var indexToAddTo = positionInfo.Type == PositionType.EndOfList ? count - 1 : positionInfo.Index;
        GenericListOrJsonArrayUtilities.SetValueAt(target, indexToAddTo, convertedValue);

        errorMessage = null;
        return true;
    }

    public virtual bool TryTest(object target, string segment, JsonSerializerOptions jsonSerializerOptions, object value, out string errorMessage)
    {

        if (!TryGetListTypeArgument(target, out var typeArgument, out errorMessage))
        {
            return false;
        }

        var count = GenericListOrJsonArrayUtilities.GetCount(target);

        if (!TryGetPositionInfo(count, segment, OperationType.Replace, out var positionInfo, out errorMessage))
        {
            return false;
        }

        if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
        {
            return false;
        }

        var currentValue = GenericListOrJsonArrayUtilities.GetElementAt(target, positionInfo.Index);

        if (!JsonObject.DeepEquals(JsonSerializer.SerializeToNode(currentValue), JsonSerializer.SerializeToNode(convertedValue)))
        {
            errorMessage = Resources.FormatValueAtListPositionNotEqualToTestValue(currentValue, value, positionInfo.Index);
            return false;
        }
        else
        {
            errorMessage = null;
            return true;
        }
    }

    public virtual bool TryTraverse(object target, string segment, JsonSerializerOptions jsonSerializerOptions, out object value, out string errorMessage)
    {
        var list = target as IList;
        if (list == null)
        {
            value = null;
            errorMessage = null;
            return false;
        }

        if (!int.TryParse(segment, out var index))
        {
            value = null;
            errorMessage = Resources.FormatInvalidIndexValue(segment);
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
            value = null;
            errorMessage = Resources.FormatIndexOutOfBounds(segment);
            return false;
        }

        value = list[index];
        errorMessage = null;
        return true;
    }

    #region New Implementaiton
    //public bool TryTraverse2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    out object nextTarget,
    //    out string errorMessage)
    //{
    //    nextTarget = null;
    //    errorMessage = null;

    //    if (target is IList list && int.TryParse(segment, out int index) && index >= 0 && index < list.Count)
    //    {
    //        nextTarget = list[index];
    //        return true;
    //    }

    //    errorMessage = "Invalid list index or out of bounds.";
    //    return false;
    //}

    //public bool TryAdd2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    object value,
    //    out string errorMessage)
    //{
    //    errorMessage = null;

    //    if (target is IList list)
    //    {
    //        if (target.GetType().IsArray)
    //        {
    //            errorMessage = $"The type '{target.GetType().FullName}' which is an array is not supported for json patch operations as it has a fixed size.";
    //            return false;
    //        }

    //        if (!TryGetListTypeArgument(list, out var typeArgument, out errorMessage))
    //        {
    //            return false;
    //        }

    //        try
    //        {
    //            object convertedValue = value;
    //            if (value != null)
    //            {
    //                convertedValue = ConvertValue2(value, typeArgument, jsonSerializerOptions, out errorMessage);
    //                if (convertedValue == null)
    //                {
    //                    return false;
    //                }
    //            }

    //            if (segment == "-") // Appending to the list
    //            {
    //                list.Add(convertedValue);
    //                return true;
    //            }

    //            if (int.TryParse(segment, out int index))
    //            {
    //                if (index >= 0 && index <= list.Count)
    //                {
    //                    list.Insert(index, convertedValue);
    //                    return true;
    //                }
    //                else
    //                {
    //                    errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
    //                    return false;
    //                }
    //            }
    //            else
    //            {
    //                errorMessage = $"The path segment '{segment}' is invalid for an array index.";
    //                return false;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            errorMessage = $"Error converting value: {ex.Message}";
    //            return false;
    //        }
    //    }

    //    errorMessage = "Invalid index for add operation.";
    //    return false;
    //}

    //public bool TryRemove2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    out string errorMessage)
    //{
    //    errorMessage = null;

    //    if (target is IList list)
    //    {
    //        if (int.TryParse(segment, out int index))
    //        {
    //            if (index >= 0 && index < list.Count)
    //            {
    //                list.RemoveAt(index);
    //                return true;
    //            }
    //            else
    //            {
    //                errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            errorMessage = $"The path segment '{segment}' is invalid for an array index.";
    //            return false;
    //        }
    //    }

    //    errorMessage = "Invalid index for remove operation.";
    //    return false;
    //}

    //public bool TryGet2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    out object value,
    //    out string errorMessage)
    //{
    //    value = null;
    //    errorMessage = null;

    //    if (target is IList list)
    //    {
    //        if (int.TryParse(segment, out int index))
    //        {
    //            if (index >= 0 && index < list.Count)
    //            {
    //                value = list[index];
    //                return true;
    //            }
    //            else
    //            {
    //                errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            errorMessage = $"The path segment '{segment}' is invalid for an array index.";
    //            return false;
    //        }
    //    }

    //    errorMessage = "Invalid index for get operation.";
    //    return false;
    //}

    //public bool TryReplace2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    object value,
    //    out string errorMessage)
    //{
    //    errorMessage = null;

    //    if (target is IList list)
    //    {
    //        if (!TryGetListTypeArgument(list, out var typeArgument, out errorMessage))
    //        {
    //            return false;
    //        }

    //        if (int.TryParse(segment, out int index))
    //        {
    //            if (index >= 0 && index < list.Count)
    //            {
    //                try
    //                {
    //                    object convertedValue = ConvertValue2(value, typeArgument, jsonSerializerOptions, out errorMessage);
    //                    if (convertedValue == null)
    //                    {
    //                        return false;
    //                    }
    //                    list[index] = convertedValue;
    //                    return true;
    //                }
    //                catch (Exception ex)
    //                {
    //                    errorMessage = $"Error converting value: {ex.Message}";
    //                    return false;
    //                }
    //            }
    //            else
    //            {
    //                errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            errorMessage = $"The path segment '{segment}' is invalid for an array index.";
    //            return false;
    //        }
    //    }

    //    errorMessage = "Invalid index for replace operation.";
    //    return false;
    //}

    //public bool TryTest2(
    //    object target,
    //    string segment,
    //    JsonSerializerOptions jsonSerializerOptions,
    //    object value,
    //    out string errorMessage)
    //{
    //    errorMessage = null;

    //    if (target is IList list && int.TryParse(segment, out int index) && index >= 0 && index < list.Count)
    //    {
    //        try
    //        {
    //            object convertedValue = ConvertValue2(value, list.GetType().GetGenericArguments()[0], jsonSerializerOptions, out errorMessage);
    //            if (convertedValue == null)
    //            {
    //                return false;
    //            }
    //            if (JsonSerializer.Serialize(list[index], jsonSerializerOptions) == JsonSerializer.Serialize(convertedValue, jsonSerializerOptions))
    //            {
    //                return true;
    //            }
    //            else
    //            {
    //                errorMessage = "Test operation failed. Values do not match.";
    //                return false;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            errorMessage = $"Error converting value: {ex.Message}";
    //            return false;
    //        }
    //    }

    //    errorMessage = "Invalid index for test operation.";
    //    return false;
    //}

    //private object ConvertValue2(object value, Type targetType, JsonSerializerOptions jsonSerializerOptions, out string errorMessage)
    //{
    //    errorMessage = null;
    //    if (value == null || targetType.IsInstanceOfType(value))
    //    {
    //        return value;
    //    }

    //    try
    //    {
    //        return Convert.ChangeType(value, targetType);
    //    }
    //    catch (Exception)
    //    {
    //        try
    //        {
    //            string json = JsonSerializer.Serialize(value, jsonSerializerOptions);
    //            return JsonSerializer.Deserialize(json, targetType, jsonSerializerOptions);
    //        }
    //        catch (Exception)
    //        {
    //            errorMessage = $"The value '{value}' is invalid for target location.";
    //            return null;
    //        }
    //    }
    //}

    #endregion

    protected virtual bool TryConvertValue(object originalValue, Type listTypeArgument, string segment, out object convertedValue, out string errorMessage)
    {
        return TryConvertValue(originalValue, listTypeArgument, segment, null, out convertedValue, out errorMessage);
    }

    protected virtual bool TryConvertValue(object originalValue, Type listTypeArgument, string segment, JsonSerializerOptions jsonSerializerOptions, out object convertedValue, out string errorMessage)
    {
        var conversionResult = ConversionResultProvider.ConvertTo(originalValue, listTypeArgument, jsonSerializerOptions);
        if (!conversionResult.CanBeConverted)
        {
            convertedValue = null;
            errorMessage = Resources.FormatInvalidValueForProperty(originalValue);
            return false;
        }

        convertedValue = conversionResult.ConvertedInstance;
        errorMessage = null;
        return true;
    }

    private static bool TryGetListTypeArgument(object list, out Type listTypeArgument, out string errorMessage)
    {
        var listType = list.GetType();
        if (listType.IsArray)
        {
            errorMessage = $"The type '{listType.FullName}' which is an array is not supported for json patch operations as it has a fixed size.";
            listTypeArgument = null;
            return false;
        }

        var genericList = ClosedGenericMatcher.ExtractGenericInterface(listType, typeof(IList<>));
        if (genericList == null)
        {
            errorMessage = $"The type '{listType.FullName}' which is a non generic list is not supported for json patch operations. Only generic list types are supported.";
            listTypeArgument = null;
            return false;
        }

        listTypeArgument = genericList.GenericTypeArguments[0];
        errorMessage = null;
        return true;
    }

    protected virtual bool TryGetPositionInfo(int collectionCount, string segment, OperationType operationType, out PositionInfo positionInfo, out string errorMessage)
    {
        if (segment == "-")
        {
            positionInfo = new PositionInfo(PositionType.EndOfList, -1);
            errorMessage = null;
            return true;
        }

        if (int.TryParse(segment, out var position))
        {
            if (position >= 0 && position < collectionCount)
            {
                positionInfo = new PositionInfo(PositionType.Index, position);
                errorMessage = null;
                return true;
            }

            // As per JSON Patch spec, for Add operation the index value representing the number of elements is valid,
            // where as for other operations like Remove, Replace, Move and Copy the target index MUST exist.
            if (position == collectionCount && operationType == OperationType.Add)
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }

            positionInfo = new PositionInfo(PositionType.OutOfBounds, position);
            errorMessage = Resources.FormatIndexOutOfBounds(segment);
            return false;
        }
        else
        {
            positionInfo = new PositionInfo(PositionType.Invalid, -1);
            errorMessage = Resources.FormatInvalidIndexValue(segment);
            return false;
        }
    }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    protected readonly struct PositionInfo
    {
        public PositionInfo(PositionType type, int index)
        {
            Type = type;
            Index = index;
        }

        public PositionType Type { get; }
        public int Index { get; }
    }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    protected enum PositionType
    {
        Index, // valid index
        EndOfList, // '-'
        Invalid, // Ex: not an integer
        OutOfBounds
    }

    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    protected enum OperationType
    {
        Add,
        Remove,
        Get,
        Replace
    }
}
