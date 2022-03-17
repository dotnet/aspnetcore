// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.Net.Http.Headers;

// List<T> allows 'null' values to be added. This is not what we want so we use a custom Collection<T> derived
// type to throw if 'null' gets added. Collection<T> internally uses List<T> which comes at some cost. In addition
// Collection<T>.Add() calls List<T>.InsertItem() which is an O(n) operation (compared to O(1) for List<T>.Add()).
// This type is only used for very small collections (1-2 items) to keep the impact of using Collection<T> small.
internal sealed class ObjectCollection<T> : Collection<T>
{
    internal static readonly Action<T> DefaultValidator = CheckNotNull;
    internal static readonly ObjectCollection<T> EmptyReadOnlyCollection
        = new ObjectCollection<T>(DefaultValidator, isReadOnly: true);

    private readonly Action<T> _validator;

    // We need to create a 'read-only' inner list for Collection<T> to do the right
    // thing.
    private static IList<T> CreateInnerList(bool isReadOnly, IEnumerable<T>? other = null)
    {
        var list = other == null ? new List<T>() : new List<T>(other);
        if (isReadOnly)
        {
            return new ReadOnlyCollection<T>(list);
        }
        else
        {
            return list;
        }
    }

    public ObjectCollection()
        : this(DefaultValidator)
    {
    }

    public ObjectCollection(Action<T> validator, bool isReadOnly = false)
        : base(CreateInnerList(isReadOnly))
    {
        _validator = validator;
    }

    public ObjectCollection(IEnumerable<T> other, bool isReadOnly = false)
        : base(CreateInnerList(isReadOnly, other))
    {
        _validator = DefaultValidator;
        foreach (T item in Items)
        {
            _validator(item);
        }
    }

    public bool IsReadOnly => ((ICollection<T>)this).IsReadOnly;

    protected override void InsertItem(int index, T item)
    {
        _validator(item);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, T item)
    {
        _validator(item);
        base.SetItem(index, item);
    }

    private static void CheckNotNull(T item)
    {
        // null values cannot be added to the collection.
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }
    }
}
