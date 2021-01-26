// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    internal class ValidationStack
    {
        public int Count => HashSet?.Count ?? List.Count;

        // We tested the performance of a list at size 15 and found it still better than hashset, but to avoid a costly
        // O(n) search at larger n we set the cutoff to 20. If someone finds the point where they intersect feel free to change this number.
        internal const int CutOff = 20;

        internal List<object> List { get; } = new List<object>();

        internal HashSet<object>? HashSet { get; set; }

        public bool Push(object model)
        {
            if (HashSet != null)
            {
                return HashSet.Add(model);
            }

            if (ListContains(model))
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
                    Debug.Assert(ReferenceEquals(List[List.Count - 1], model));
                    List.RemoveAt(List.Count - 1);
                }
            }
        }

        private bool ListContains(object model)
        {
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
}
