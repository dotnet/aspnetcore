// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal sealed class ConditionCollection : IEnumerable<Condition>
{
    private readonly List<Condition> _conditions = new List<Condition>();

    public LogicalGrouping Grouping { get; }
    public bool TrackAllCaptures { get; }

    public ConditionCollection()
        : this(LogicalGrouping.MatchAll, trackAllCaptures: false)
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
            throw new ArgumentOutOfRangeException(null, $"Cannot access condition at index {index}. Only {_conditions.Count} conditions were captured.");
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
