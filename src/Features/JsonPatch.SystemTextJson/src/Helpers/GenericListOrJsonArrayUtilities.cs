// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Helpers;

internal static class GenericListOrJsonArrayUtilities
{
    internal static object GetElementAt(object list, int index)
    {
        if (list is IList nonGenericList)
        {
            return nonGenericList[index];
        }

        if (list is JsonArray array)
        {
            return array[index];
        }

        throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
    }

    internal static void SetValueAt(object list, int index, object value)
    {
        if (list is IList nonGenericList)
        {
            nonGenericList[index] = value;
        }
        else if (list is JsonArray array)
        {
            array[index] = (JsonNode)value;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
        }
    }

    internal static int GetCount(object list)
    {
        if (list is ICollection nonGenericList)
        {
            return nonGenericList.Count;
        }

        if (list is JsonArray jsonArray)
        {
            return jsonArray.Count;
        }

        throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
    }

    internal static void RemoveElementAt(object list, int index)
    {
        if (list is IList nonGenericList)
        {
            nonGenericList.RemoveAt(index);
        }
        else if (list is JsonArray array)
        {
            array.RemoveAt(index);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
        }
    }

    internal static void InsertElementAt(object list, int index, object value)
    {
        if (list is IList nonGenericList)
        {
            nonGenericList.Insert(index, value);
        }
        else if (list is JsonArray array)
        {
            array.Insert(index, (JsonNode)value);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
        }
    }

    internal static void AddElement(object list, object value)
    {
        if (list is IList nonGenericList)
        {
            nonGenericList.Add(value);
        }
        else if (list is JsonArray array)
        {
            array.Add(value);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported list type: {list.GetType()}");
        }
    }
}
