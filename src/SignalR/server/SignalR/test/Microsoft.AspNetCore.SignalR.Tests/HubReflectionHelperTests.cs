// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class HubReflectionHelperTests
{
    [Fact]
    public void EmptyHubHasNoHubMethods()
    {
        var hubMethods = HubReflectionHelper.GetHubMethods(typeof(EmptyHub));

        Assert.Empty(hubMethods);
    }

    [Fact]
    public void HubWithMethodsHasHubMethods()
    {
        var hubType = typeof(BaseMethodHub);
        var hubMethods = HubReflectionHelper.GetHubMethods(hubType);

        Assert.Equal(3, hubMethods.Count());
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("VoidMethod"));
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("IntMethod"));
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("ArgMethod"));
    }

    [Fact]
    public void InheritedHubHasBaseHubMethodsAndOwnMethods()
    {
        var hubType = typeof(InheritedMethodHub);
        var hubMethods = HubReflectionHelper.GetHubMethods(hubType);

        Assert.Equal(4, hubMethods.Count());
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("ExtraMethod"));
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("VoidMethod"));
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("IntMethod"));
        Assert.Contains(hubMethods, m => m == hubType.GetMethod("ArgMethod"));
    }

    private class EmptyHub : Hub
    {
    }

    private class BaseMethodHub : Hub
    {
        public void VoidMethod()
        {
        }

        public int IntMethod()
        {
            return 0;
        }

        public void ArgMethod(string str)
        {
        }

        // static is not supported as a Hub method
        public static void StaticMethod()
        {
        }

        // internal is not a Hub method
        internal void InternalMethod()
        {
        }

        // private is not a Hub method
        private void PrivateMethod()
        {
        }
    }

    private class InheritedMethodHub : BaseMethodHub
    {
        public int ExtraMethod(bool b)
        {
            return 2;
        }
    }
}
