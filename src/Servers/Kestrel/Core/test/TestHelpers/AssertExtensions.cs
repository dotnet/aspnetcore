// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Sdk;

namespace Xunit;

public static class AssertExtensions
{
    public static void Equal(byte[] expected, Span<byte> actual)
    {
        if (expected.Length != actual.Length)
        {
            throw new XunitException($"Expected length to be {expected.Length} but was {actual.Length}");
        }

        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new XunitException($@"Expected byte at index {i} to be '{expected[i]}' but was '{actual[i]}'");
            }
        }
    }
}
