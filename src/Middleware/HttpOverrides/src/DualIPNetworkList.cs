// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPDEPR005 // Type or member is obsolete

using AspNetIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
using IPAddress = System.Net.IPAddress;
using IPNetwork = System.Net.IPNetwork;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Internal list implementation that keeps <see cref="System.Net.IPNetwork"/> and the obsolete
/// <see cref="Microsoft.AspNetCore.HttpOverrides.IPNetwork"/> collections in sync. Modifications
/// through either interface are reflected in the other.
/// </summary>
internal sealed class DualIPNetworkList : IList<IPNetwork>, IList<AspNetIPNetwork>
{
    // Two independent underlying lists so each side behaves exactly like a List<T> with respect to
    // enumeration versioning, capacity growth, etc. They are kept strictly in sync by all mutating operations.
    private readonly List<IPNetwork> _system = new();
    private readonly List<AspNetIPNetwork> _aspnet = new();

    public DualIPNetworkList()
    {
        // Default entry (loopback) added to both representations.
        var loopback = new IPNetwork(IPAddress.Loopback, 8);
        _system.Add(loopback);
        _aspnet.Add(new AspNetIPNetwork(loopback.BaseAddress, loopback.PrefixLength));
    }

    int ICollection<IPNetwork>.Count => _system.Count;
    int ICollection<AspNetIPNetwork>.Count => _aspnet.Count;

    bool ICollection<IPNetwork>.IsReadOnly => false;
    bool ICollection<AspNetIPNetwork>.IsReadOnly => false;

    IPNetwork IList<IPNetwork>.this[int index]
    {
        get => _system[index];
        set
        {
            _system[index] = value;
            _aspnet[index] = new AspNetIPNetwork(value.BaseAddress, value.PrefixLength);
        }
    }

    AspNetIPNetwork IList<AspNetIPNetwork>.this[int index]
    {
        get => _aspnet[index];
        set
        {
            _aspnet[index] = value;
            _system[index] = new IPNetwork(value.Prefix, value.PrefixLength);
        }
    }

    void ICollection<IPNetwork>.Add(IPNetwork item)
    {
        _system.Add(item);
        _aspnet.Add(new AspNetIPNetwork(item.BaseAddress, item.PrefixLength));
    }

    void ICollection<AspNetIPNetwork>.Add(AspNetIPNetwork item)
    {
        _aspnet.Add(item);
        _system.Add(new IPNetwork(item.Prefix, item.PrefixLength));
    }

    public void Clear()
    {
        _system.Clear();
        _aspnet.Clear();
    }

    void ICollection<IPNetwork>.Clear() => Clear();
    void ICollection<AspNetIPNetwork>.Clear() => Clear();

    bool ICollection<IPNetwork>.Contains(IPNetwork item) => _system.Contains(item);
    bool ICollection<AspNetIPNetwork>.Contains(AspNetIPNetwork item) => _aspnet.Contains(item);

    public void CopyTo(IPNetwork[] array, int arrayIndex) => _system.CopyTo(array, arrayIndex);
    public void CopyTo(AspNetIPNetwork[] array, int arrayIndex) => _aspnet.CopyTo(array, arrayIndex);

    void ICollection<IPNetwork>.CopyTo(IPNetwork[] array, int arrayIndex) => CopyTo(array, arrayIndex);
    void ICollection<AspNetIPNetwork>.CopyTo(AspNetIPNetwork[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _system.GetEnumerator();

    IEnumerator<IPNetwork> IEnumerable<IPNetwork>.GetEnumerator() => _system.GetEnumerator();
    IEnumerator<AspNetIPNetwork> IEnumerable<AspNetIPNetwork>.GetEnumerator() => _aspnet.GetEnumerator();

    int IList<IPNetwork>.IndexOf(IPNetwork item) => _system.IndexOf(item);
    int IList<AspNetIPNetwork>.IndexOf(AspNetIPNetwork item) => _aspnet.IndexOf(item);

    void IList<IPNetwork>.Insert(int index, IPNetwork item)
    {
        _system.Insert(index, item);
        _aspnet.Insert(index, new AspNetIPNetwork(item.BaseAddress, item.PrefixLength));
    }

    void IList<AspNetIPNetwork>.Insert(int index, AspNetIPNetwork item)
    {
        _aspnet.Insert(index, item);
        _system.Insert(index, new IPNetwork(item.Prefix, item.PrefixLength));
    }

    bool ICollection<IPNetwork>.Remove(IPNetwork item)
    {
        var idx = _system.IndexOf(item);
        if (idx >= 0)
        {
            RemoveAt(idx);
            return true;
        }
        return false;
    }

    bool ICollection<AspNetIPNetwork>.Remove(AspNetIPNetwork item)
    {
        var idx = _aspnet.IndexOf(item);
        if (idx >= 0)
        {
            RemoveAt(idx);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        _system.RemoveAt(index);
        _aspnet.RemoveAt(index);
    }

    void IList<IPNetwork>.RemoveAt(int index) => RemoveAt(index);
    void IList<AspNetIPNetwork>.RemoveAt(int index) => RemoveAt(index);
}

#pragma warning restore ASPDEPR005 // Type or member is obsolete
