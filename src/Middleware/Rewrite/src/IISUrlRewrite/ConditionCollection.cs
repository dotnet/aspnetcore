// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class ConditionCollection : IEnumerable<Condition>
    {
        private readonly List<Condition> _conditions = new List<Condition>();

        public LogicalGrouping Grouping { get; }
        public bool TrackAllCaptures { get; }

        public ConditionCollection()
            :this(LogicalGrouping.MatchAll, trackAllCaptures: false)
        {
        }

        public ConditionCollection(LogicalGrouping grouping, bool trackAllCaptures)
        {
            Grouping = grouping;
            TrackAllCaptures = trackAllCaptures;
        }

        public int Count => _conditions.Count;

        public Condition this[int index]
        {
            get
            {
                if (index < _conditions.Count)
                {
                    return _conditions[index];
                }
                throw new IndexOutOfRangeException($"Cannot access condition at index {index}. Only {_conditions.Count} conditions were captured.");
            }
        }

        public void Add(Condition condition)
        {
            if (condition != null)
            {
                _conditions.Add(condition);
            }
        }

        public void AddConditions(IEnumerable<Condition> conditions)
        {
            if (conditions != null)
            {
                _conditions.AddRange(conditions);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _conditions.GetEnumerator();
        }

        public IEnumerator<Condition> GetEnumerator()
        {
            return _conditions.GetEnumerator();
        }
    }
}