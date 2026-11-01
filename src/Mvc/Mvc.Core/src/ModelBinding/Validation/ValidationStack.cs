// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

#pragma warning disable CA1852 // Seal internal types
internal class ValidationStack
#pragma warning restore CA1852 // Seal internal types
{
    public int Count => HashSet?.Count ?? List?.Count ?? (First is null ? 0 : 1);

    // We tested the performance of a list at size 15 and found it still better than hashset, but to avoid a costly
    // O(n) search at larger n we set the cutoff to 20. If someone finds the point where they intersect feel free to change this number.
    internal const int CutOff = 20;

    // Most validated models are never nested (for example simple action parameters), so the first model is stored
    // inline and the list is only created once a second model is pushed.
    internal object? First { get; private set; }

    internal List<object>? List { get; private set; }

    internal HashSet<object>? HashSet { get; set; }

    public bool Push(object model)
    {
        if (HashSet != null)
        {
            return HashSet.Add(model);
        }

        if (List == null)
        {
            if (First == null)
            {
                First = model;
                return true;
            }

            if (ReferenceEquals(First, model))
            {
                return false;
            }

            List = new List<object> { First };
            First = null;
        }
        else if (ListContains(model))
        {
            return false;
        }

        List.Add(model);

        if (HashSet == null && List.Count > CutOff)
        {
            HashSet = new HashSet<object>(List, ReferenceEqualityComparer.Instance);
        }

        return true;
    }

    public void Pop(object? model)
    {
        if (HashSet != null)
        {
            HashSet.Remove(model!);
        }
        else
        {
            if (model != null)
            {
                if (List == null)
                {
                    Debug.Assert(ReferenceEquals(First, model));
                    First = null;
                }
                else
                {
                    Debug.Assert(ReferenceEquals(List[List.Count - 1], model));
                    List.RemoveAt(List.Count - 1);
                }
            }
        }
    }

    private bool ListContains(object model)
    {
        Debug.Assert(List != null);

        for (var i = 0; i < List.Count; i++)
        {
            if (ReferenceEquals(model, List[i]))
            {
                return true;
            }
        }

        return false;
    }
}
