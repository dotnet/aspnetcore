// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class CustomObject : IEquatable<CustomObject>
{
    // Not intended to be a full set of things, just a smattering of sample serializations
    public string StringProp { get; set; } = "SignalR!";

    public double DoubleProp { get; set; } = 6.2831853071;

    public int IntProp { get; set; } = 42;

    public DateTime DateTimeProp { get; set; } = new DateTime(2017, 4, 11, 0, 0, 0, DateTimeKind.Utc);

    public object NullProp { get; set; } = null;

    public byte[] ByteArrProp { get; set; } = new byte[] { 1, 2, 3 };

    public override bool Equals(object obj)
    {
        return obj is CustomObject o && Equals(o);
    }

    public override int GetHashCode()
    {
        // This is never used in a hash table
        return 0;
    }

    public bool Equals(CustomObject right)
    {
        // This allows the comparer below to properly compare the object in the test.
        return string.Equals(StringProp, right.StringProp, StringComparison.Ordinal) &&
            DoubleProp == right.DoubleProp &&
            IntProp == right.IntProp &&
            DateTime.Equals(DateTimeProp, right.DateTimeProp) &&
            NullProp == right.NullProp &&
            System.Linq.Enumerable.SequenceEqual(ByteArrProp, right.ByteArrProp);
    }
}
