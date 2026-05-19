// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

/// <remarks>
/// <para>
/// This class is used specifically to test that JSON patch "replace" operations are functionally equivalent to
/// "add" and "remove" operations applied sequentially using the same path.
/// </para>
/// <para>
/// This is done by asserting that  no value exists for a particular key before setting its value. To replace the
/// value for a key, the key must first be removed, and then re-added with the new value.
/// </para>
/// <para>
/// See JsonPatch#110 for further details.
/// </para>
/// </remarks>
public class WriteOnceDynamicTestObject : DynamicObject
{
    private Dictionary<string, object> _dictionary = new Dictionary<string, object>();

    public object this[string key] { get => ((IDictionary<string, object>)_dictionary)[key]; set => SetValueForKey(key, value); }

    public ICollection<string> Keys => ((IDictionary<string, object>)_dictionary).Keys;

    public ICollection<object> Values => ((IDictionary<string, object>)_dictionary).Values;

    public int Count => ((IDictionary<string, object>)_dictionary).Count;

    public bool IsReadOnly => ((IDictionary<string, object>)_dictionary).IsReadOnly;

    public void Add(string key, object value)
    {
        SetValueForKey(key, value);
    }

    public void Add(KeyValuePair<string, object> item)
    {
        SetValueForKey(item.Key, item.Value);
    }

    public void Clear()
    {
        ((IDictionary<string, object>)_dictionary).Clear();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
        return ((IDictionary<string, object>)_dictionary).Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return ((IDictionary<string, object>)_dictionary).ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        ((IDictionary<string, object>)_dictionary).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return ((IDictionary<string, object>)_dictionary).GetEnumerator();
    }

    public bool Remove(string key)
    {
        return ((IDictionary<string, object>)_dictionary).Remove(key);
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
        return ((IDictionary<string, object>)_dictionary).Remove(item);
    }

    public bool TryGetValue(string key, out object value)
    {
        return ((IDictionary<string, object>)_dictionary).TryGetValue(key, out value);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        var name = binder.Name;

        return TryGetValue(name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        SetValueForKey(binder.Name, value);

        return true;
    }

    private void SetValueForKey(string key, object value)
    {
        if (value == null)
        {
            _dictionary.Remove(key);
            return;
        }

        if (_dictionary.ContainsKey(key))
        {
            throw new ArgumentException($"Value for {key} already exists");
        }

        _dictionary[key] = value;
    }
}
