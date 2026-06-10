// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Xunit;
using AspNetIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Microsoft.AspNetCore.HttpOverrides;

public class DualIPNetworkListTests
{
    [Fact]
    public void DefaultContainsLoopback()
    {
        var options = new ForwardedHeadersOptions();
        Assert.Single(options.KnownIPNetworks);
        Assert.Equal("127.0.0.0", options.KnownIPNetworks[0].BaseAddress.ToString());
        Assert.Equal(8, options.KnownIPNetworks[0].PrefixLength);
#pragma warning disable ASPDEPR005
        Assert.Single(options.KnownNetworks);
        Assert.Equal("127.0.0.0", options.KnownNetworks[0].Prefix.ToString());
        Assert.Equal(8, options.KnownNetworks[0].PrefixLength);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void AddThroughSystemCollectionVisibleViaObsolete()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
#pragma warning disable ASPDEPR005
        var obsoleteList = options.KnownNetworks;
        Assert.Equal(2, obsoleteList.Count);
        Assert.Equal(IPAddress.Parse("10.0.0.0"), obsoleteList[1].Prefix);
        Assert.Equal(8, obsoleteList[1].PrefixLength);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void AddThroughObsoleteCollectionVisibleViaSystem()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks.Add(new AspNetIPNetwork(IPAddress.Parse("192.168.0.0"), 16));
        Assert.Equal(2, options.KnownIPNetworks.Count);
        Assert.Equal("192.168.0.0/16", options.KnownIPNetworks[1].ToString());
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void ReplaceViaSystemIndexerUpdatesObsolete()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks[0] = System.Net.IPNetwork.Parse("172.16.0.0/12");
#pragma warning disable ASPDEPR005
        Assert.Equal(IPAddress.Parse("172.16.0.0"), options.KnownNetworks[0].Prefix);
        Assert.Equal(12, options.KnownNetworks[0].PrefixLength);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void ReplaceViaObsoleteIndexerUpdatesSystem()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks[0] = new AspNetIPNetwork(IPAddress.Parse("172.16.0.0"), 12);
        Assert.Equal("172.16.0.0/12", options.KnownIPNetworks[0].ToString());
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void ClearClearsBoth()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Clear();
#pragma warning disable ASPDEPR005
        Assert.Empty(options.KnownNetworks);
#pragma warning restore ASPDEPR005
        Assert.Empty(options.KnownIPNetworks);
    }

    [Fact]
    public void RemoveThroughEitherCollectionRemovesFromBoth()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        var first = options.KnownIPNetworks[0];
        var removed = options.KnownIPNetworks.Remove(first);
        Assert.True(removed);
#pragma warning disable ASPDEPR005
        var obsoleteList = options.KnownNetworks;
        Assert.DoesNotContain(obsoleteList, n => n.Prefix.Equals(IPAddress.Loopback));
#pragma warning restore ASPDEPR005
        Assert.Single(options.KnownIPNetworks); // only the 10.0.0.0/8 entry should remain
    }

    // New tests to cover each IList<T> member for both interfaces

    [Fact]
    public void ContainsWorksForBothLists()
    {
        var options = new ForwardedHeadersOptions();
        var loopback = options.KnownIPNetworks[0];
        Assert.Contains(loopback, options.KnownIPNetworks);
#pragma warning disable ASPDEPR005
        Assert.Contains(options.KnownNetworks, n => n.Prefix.Equals(loopback.BaseAddress) && n.PrefixLength == loopback.PrefixLength);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void CopyToSystem()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        var arr = new System.Net.IPNetwork[5];
        options.KnownIPNetworks.CopyTo(arr, 1);
        Assert.Equal("127.0.0.0/8", arr[1].ToString());
        Assert.Equal("10.0.0.0/8", arr[2].ToString());
    }

    [Fact]
    public void CopyToObsolete()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks.Add(new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        var arr = new AspNetIPNetwork[5];
        options.KnownNetworks.CopyTo(arr, 2);
        Assert.Null(arr[0]);
        Assert.Equal(IPAddress.Parse("127.0.0.0"), arr[2].Prefix);
        Assert.Equal(8, arr[2].PrefixLength);
        Assert.Equal(IPAddress.Parse("10.0.0.0"), arr[3].Prefix);
        Assert.Equal(8, arr[3].PrefixLength);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void IndexOfSystem()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        Assert.Equal(1, options.KnownIPNetworks.IndexOf(System.Net.IPNetwork.Parse("10.0.0.0/8")));
    }

    [Fact]
    public void IndexOfObsolete()
    {
        // AspNetIPNetwork doesn't implement Equals, so IndexOf uses reference equality.
        // This keeps the obsolete behavior intact.

#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        var item = new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8);
        options.KnownNetworks.Add(item);
        Assert.Equal(-1, options.KnownNetworks.IndexOf(new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8)));
        Assert.Equal(1, options.KnownNetworks.IndexOf(item));
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void InsertSystem()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Insert(0, System.Net.IPNetwork.Parse("10.0.0.0/8"));
        Assert.Equal("10.0.0.0/8", options.KnownIPNetworks[0].ToString());
#pragma warning disable ASPDEPR005
        Assert.Equal(IPAddress.Parse("10.0.0.0"), options.KnownNetworks[0].Prefix);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void InsertObsolete()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks.Insert(0, new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        Assert.Equal("10.0.0.0/8", options.KnownIPNetworks[0].ToString());
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void RemoveAtSystem()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        options.KnownIPNetworks.RemoveAt(0); // remove loopback
#pragma warning disable ASPDEPR005
        Assert.DoesNotContain(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Loopback));
#pragma warning restore ASPDEPR005
        Assert.Single(options.KnownIPNetworks); // only 10.0.0.0/8
    }

    [Fact]
    public void RemoveAtObsolete()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks.Add(new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        options.KnownNetworks.RemoveAt(0); // remove loopback
        Assert.DoesNotContain(options.KnownIPNetworks, n => n.BaseAddress.Equals(IPAddress.Loopback));
        Assert.Single(options.KnownIPNetworks); // only 10.0.0.0/8
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void EnumerateSystem()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        var list = options.KnownIPNetworks.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, n => n.BaseAddress.Equals(IPAddress.Parse("10.0.0.0")));
    }

    [Fact]
    public void EnumerateObsolete()
    {
#pragma warning disable ASPDEPR005
        var options = new ForwardedHeadersOptions();
        options.KnownNetworks.Add(new AspNetIPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        var list = options.KnownNetworks.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, n => n.Prefix.Equals(IPAddress.Parse("10.0.0.0")));
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void IsReadOnlyFalse()
    {
        var options = new ForwardedHeadersOptions();
        Assert.False(options.KnownIPNetworks.IsReadOnly);
#pragma warning disable ASPDEPR005
        Assert.False(options.KnownNetworks.IsReadOnly);
#pragma warning restore ASPDEPR005
    }

    [Fact]
    public void CountSyncAfterMixedOperations()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
#pragma warning disable ASPDEPR005
        options.KnownNetworks.Add(new AspNetIPNetwork(IPAddress.Parse("192.168.0.0"), 16));
        Assert.Equal(options.KnownIPNetworks.Count, options.KnownNetworks.Count);
#pragma warning restore ASPDEPR005
    }
}
